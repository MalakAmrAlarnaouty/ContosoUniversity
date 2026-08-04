using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data;

public class SchoolContext : DbContext
{
    public SchoolContext(
        DbContextOptions<SchoolContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; } = null!;

    public DbSet<Course> Courses { get; set; } = null!;

    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    public DbSet<Department> Departments { get; set; } = null!;

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Course>()
            .HasOne(course => course.Department)
            .WithMany(department => department.Courses)
            .HasForeignKey(course => course.DepartmentID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}