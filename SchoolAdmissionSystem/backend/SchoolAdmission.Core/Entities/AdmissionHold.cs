using SchoolAdmission.API.Enums;
namespace SchoolAdmission.Core.Entities;

public class AdmissionHold
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClassId { get; set; }

    public SchoolClass? SchoolClass { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public HoldStatus Status { get; set; } = HoldStatus.HELD;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
}