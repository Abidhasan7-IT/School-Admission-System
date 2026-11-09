using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Enums;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ClassesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // POST /classes
    [HttpPost]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.TotalSeats <= 0)
        {
            return BadRequest("Invalid class name or seat capacity.");
        }

        var schoolClass = new SchoolClass
        {
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats
        };

        _db.Classes.Add(schoolClass);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetClass),
            new { classId = schoolClass.Id },
            schoolClass
        );
    }

    // GET /classes
    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        var now = DateTime.UtcNow;

        var classes = await _db.Classes
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,

                HeldSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.HELD &&
                    h.ExpiresAt > now),

                ConfirmedSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.CONFIRMED)
            })
            .ToListAsync();

        var result = classes.Select(c => new
        {
            c.Id,
            c.Name,
            c.TotalSeats,
            c.HeldSeats,
            c.ConfirmedSeats,
            AvailableSeats = Math.Max(
                0,
                c.TotalSeats - c.HeldSeats - c.ConfirmedSeats)
        });

        return Ok(result);
    }

    // GET /classes/{classId}
    [HttpGet("{classId:guid}")]
    public async Task<IActionResult> GetClass(Guid classId)
    {
        var now = DateTime.UtcNow;

        var schoolClass = await _db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,

                HeldSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.HELD &&
                    h.ExpiresAt > now),

                ConfirmedSeats = c.AdmissionHolds.Count(h =>
                    h.Status == HoldStatus.CONFIRMED)
            })
            .FirstOrDefaultAsync();

        if (schoolClass == null)
            return NotFound("Class not found.");

        return Ok(new
        {
            schoolClass.Id,
            schoolClass.Name,
            schoolClass.TotalSeats,
            schoolClass.HeldSeats,
            schoolClass.ConfirmedSeats,
            AvailableSeats = Math.Max(
                0,
                schoolClass.TotalSeats -
                schoolClass.HeldSeats -
                schoolClass.ConfirmedSeats)
        });
    }
}

public class CreateClassRequest
{
    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }
}