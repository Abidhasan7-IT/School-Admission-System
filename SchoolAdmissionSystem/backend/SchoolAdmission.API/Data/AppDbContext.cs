using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Column names are explicit snake_case because some queries are written as raw SQL.
        modelBuilder.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id").HasMaxLength(64);
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(c => c.TotalSeats).HasColumnName("total_seats");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id").HasMaxLength(64);
            e.Property(h => h.ClassId).HasColumnName("class_id").HasMaxLength(64).IsRequired();
            e.Property(h => h.StudentId).HasColumnName("student_id").HasMaxLength(20).IsRequired();
            e.Property(h => h.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.CreatedAt).HasColumnName("created_at");
            e.Property(h => h.ExpiresAt).HasColumnName("expires_at");

            e.HasOne(h => h.Class)
                .WithMany()
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // Database-level guarantee: a student can hold at most one active application per class.
            e.HasIndex(h => new { h.ClassId, h.StudentId })
                .IsUnique()
                .HasDatabaseName("ux_holds_active_per_class_student")
                .HasFilter("status IN ('HELD', 'CONFIRMED')");
        });
    }
}
