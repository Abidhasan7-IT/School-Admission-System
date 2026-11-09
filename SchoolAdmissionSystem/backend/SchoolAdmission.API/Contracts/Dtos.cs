using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Contracts;

public record ErrorResponse(string Code, string Message);

public class CreateClassRequest
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

    [Range(1, 10000)]
    public int TotalSeats { get; set; }
}

public record ClassDto(string Id, string Name, int TotalSeats);

public record ClassSummaryDto(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

public record HoldCreatedDto(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt);

public record HoldStatusDto(string HoldId, string ClassId, string Status, DateTime ExpiresAt);

public record ConfirmResultDto(string HoldId, string Status);

public record MyHoldDto(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt);
