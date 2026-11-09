using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Api.Services;

   
public class AdmissionService(AppDbContext db, AdmissionOptions options)
{
    public async Task<ClassDto> CreateClassAsync(string name, int totalSeats)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Class name is required.");
        }

        if (totalSeats < 1)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "totalSeats must be at least 1.");
        }

        var cls = new SchoolClass
        {
            Id = NewId("c"),
            Name = name,
            TotalSeats = totalSeats,
            CreatedAt = DateTime.UtcNow,
        };

        db.Classes.Add(cls);
        await db.SaveChangesAsync();
        return new ClassDto(cls.Id, cls.Name, cls.TotalSeats);
    }

    public async Task<List<ClassSummaryDto>> ListClassesAsync()
    {
        var classes = await db.Classes.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var counts = await CountActiveHoldsAsync(DateTime.UtcNow, classId: null);

        return classes.Select(c => ToSummary(c, counts)).ToList();
    }

    public async Task<ClassSummaryDto> GetClassAsync(string classId)
    {
        var cls = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "CLASS_NOT_FOUND", "Class was not found.");

        var counts = await CountActiveHoldsAsync(DateTime.UtcNow, classId);
        return ToSummary(cls, counts);
    }

    public async Task<HoldCreatedDto> CreateHoldAsync(string classId, string studentId)
    {
        var now = DateTime.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Lock this class row. Every application for the same class waits here,
            //    so the seat check below cannot race with another request.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM classes WHERE id = {classId} FOR UPDATE");

            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "CLASS_NOT_FOUND", "Class was not found.");

            // 2. Release holds whose time has run out, so their seats count as free again.
            var stale = await db.Holds
                .Where(h => h.ClassId == classId && h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ToListAsync();
            foreach (var h in stale)
            {
                h.Status = HoldStatus.EXPIRED;
            }
            await db.SaveChangesAsync();

            // 3. One active application per student per class.
            var alreadyActive = await db.Holds.AnyAsync(h =>
                h.ClassId == classId
                && h.StudentId == studentId
                && (h.Status == HoldStatus.HELD || h.Status == HoldStatus.CONFIRMED));
            if (alreadyActive)
            {
                throw new ApiException(
                    StatusCodes.Status409Conflict,
                    "ALREADY_ACTIVE",
                    "This student already has an active application or admission for this class.");
            }

            // 4. Seat check: held + confirmed must never exceed total seats.
            var used = await db.Holds.CountAsync(h =>
                h.ClassId == classId && (h.Status == HoldStatus.HELD || h.Status == HoldStatus.CONFIRMED));
            if (used >= cls.TotalSeats)
            {
                throw new ApiException(StatusCodes.Status409Conflict, "SEAT_FULL", "No seats are left in this class.");
            }

            var hold = new Hold
            {
                Id = NewId("h"),
                ClassId = classId,
                StudentId = studentId,
                Status = HoldStatus.HELD,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(options.HoldDurationSeconds),
            };

            db.Holds.Add(hold);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return new HoldCreatedDto(hold.Id, hold.ClassId, hold.StudentId, "HELD", hold.ExpiresAt);
        }
        catch (DbUpdateException)
        {
            // Backstop from the partial unique index, should the lock ever be bypassed.
            throw new ApiException(StatusCodes.Status409Conflict, "ALREADY_ACTIVE",
                "This student already has an active application or admission for this class.");
        }
    }

    public async Task<HoldStatusDto> GetHoldAsync(string holdId)
    {
        var hold = await db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "HOLD_NOT_FOUND", "Application was not found.");

        return new HoldStatusDto(hold.Id, hold.ClassId, EffectiveStatus(hold, DateTime.UtcNow), hold.ExpiresAt);
    }

    public async Task<ConfirmResultDto> ConfirmHoldAsync(string holdId, string studentId)
    {
        var now = DateTime.UtcNow;
        var hold = await FindOwnedHoldAsync(holdId, studentId);

        // Confirming twice is harmless: the second call just reports the current state.
        if (hold.Status == HoldStatus.CONFIRMED)
        {
            return new ConfirmResultDto(hold.Id, "CONFIRMED");
        }

        if (hold.Status == HoldStatus.CANCELLED)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "HOLD_NOT_ACTIVE", "This application was cancelled.");
        }

        if (hold.Status == HoldStatus.EXPIRED || hold.ExpiresAt <= now)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "HOLD_EXPIRED", "The application time limit has passed.");
        }

        // Conditional update: succeeds only if the hold is still HELD and not yet expired at this instant.
        var updated = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE holds SET status = 'CONFIRMED' WHERE id = {holdId} AND status = 'HELD' AND expires_at > {now}");

        if (updated == 0)
        {
            var latest = await db.Holds.AsNoTracking().FirstAsync(h => h.Id == holdId);
            if (latest.Status == HoldStatus.CONFIRMED)
            {
                return new ConfirmResultDto(holdId, "CONFIRMED");
            }

            throw new ApiException(StatusCodes.Status409Conflict, "HOLD_EXPIRED", "The application time limit has passed.");
        }

        return new ConfirmResultDto(holdId, "CONFIRMED");
    }

    public async Task CancelHoldAsync(string holdId, string studentId)
    {
        var hold = await FindOwnedHoldAsync(holdId, studentId);

        if (hold.Status == HoldStatus.CONFIRMED)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "HOLD_ALREADY_CONFIRMED",
                "An admission that is already confirmed cannot be cancelled.");
        }

        // Cancelled or expired holds no longer occupy a seat, so there is nothing left to do.
        if (hold.Status is HoldStatus.CANCELLED or HoldStatus.EXPIRED)
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE holds SET status = 'CANCELLED' WHERE id = {holdId} AND status = 'HELD'");
    }

    public async Task<List<MyHoldDto>> GetMyHoldsAsync(string studentId)
    {
        var now = DateTime.UtcNow;
        var holds = await db.Holds.AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .Include(h => h.Class)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return holds
            .Select(h => new MyHoldDto(h.Id, h.ClassId, h.Class!.Name, EffectiveStatus(h, now), h.ExpiresAt))
            .ToList();
    }

    private async Task<Hold> FindOwnedHoldAsync(string holdId, string studentId)
    {
        var hold = await db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "HOLD_NOT_FOUND", "Application was not found.");

        if (hold.StudentId != studentId)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "FORBIDDEN", "This application belongs to another student.");
        }

        return hold;
    }

    /// <summary>Active = confirmed, or held and not yet past its expiry time.</summary>
    private async Task<Dictionary<string, (int Held, int Confirmed)>> CountActiveHoldsAsync(DateTime now, string? classId)
    {
        var query = db.Holds.AsNoTracking()
            .Where(h => h.Status == HoldStatus.CONFIRMED || (h.Status == HoldStatus.HELD && h.ExpiresAt > now));

        if (classId is not null)
        {
            query = query.Where(h => h.ClassId == classId);
        }

        var rows = await query
            .GroupBy(h => h.ClassId)
            .Select(g => new
            {
                ClassId = g.Key,
                Held = g.Count(h => h.Status == HoldStatus.HELD),
                Confirmed = g.Count(h => h.Status == HoldStatus.CONFIRMED),
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.ClassId, r => (r.Held, r.Confirmed));
    }

    private static ClassSummaryDto ToSummary(SchoolClass cls, Dictionary<string, (int Held, int Confirmed)> counts)
    {
        var (held, confirmed) = counts.TryGetValue(cls.Id, out var c) ? c : (0, 0);
        return new ClassSummaryDto(
            cls.Id,
            cls.Name,
            cls.TotalSeats,
            held,
            confirmed,
            Math.Max(0, cls.TotalSeats - held - confirmed));
    }

    /// <summary>The stored status can lag by up to one sweep interval, so expiry is also checked on read.</summary>
    private static string EffectiveStatus(Hold hold, DateTime now) =>
        hold.Status == HoldStatus.HELD && hold.ExpiresAt <= now ? "EXPIRED" : hold.Status.ToString();

    private static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
