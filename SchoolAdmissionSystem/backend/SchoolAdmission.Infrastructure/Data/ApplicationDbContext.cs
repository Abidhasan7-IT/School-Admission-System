using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolClass>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<SchoolClass>()
            .Property(c => c.Name)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<SchoolClass>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Classes_TotalSeats",
                "\"TotalSeats\" >= 0"));

        modelBuilder.Entity<AdmissionHold>()
            .HasKey(h => h.Id);

        modelBuilder.Entity<AdmissionHold>()
            .Property(h => h.StudentId)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<AdmissionHold>()
            .HasOne(h => h.SchoolClass)
            .WithMany(c => c.AdmissionHolds)
            .HasForeignKey(h => h.ClassId);

        modelBuilder.Entity<AdmissionHold>()
            .HasIndex(h => new { h.ClassId, h.StudentId });
    }
}