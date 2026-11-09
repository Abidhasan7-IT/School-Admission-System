namespace SchoolAdmission.Core.Entities;

public class SchoolClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AdmissionHold> AdmissionHolds { get; set; }
        = new List<AdmissionHold>();
}