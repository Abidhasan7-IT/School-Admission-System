namespace SchoolAdmission.Api.Domain;

public enum HoldStatus
{
    HELD,
    CONFIRMED,
    EXPIRED,
    CANCELLED
}

public class SchoolClass
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Hold> AdmissionHolds { get; set; } = new List<Hold>();
}

public class Hold
{
    public string Id { get; set; } = default!;
    public string ClassId { get; set; } = default!;
    public SchoolClass? Class { get; set; }
    public string StudentId { get; set; } = default!;
    public HoldStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
