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

    public DbSet<Instructor> Instructors { get; set; } = null!;

    public DbSet<CourseInstructor> CourseInstructors { get; set; } = null!;

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Course>()
            .HasOne(course => course.Department)
            .WithMany(department => department.Courses)
            .HasForeignKey(course => course.DepartmentID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Instructor>()
            .HasOne(instructor => instructor.Department)
            .WithMany(department => department.Instructors)
            .HasForeignKey(instructor => instructor.DepartmentID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Student>()
            .HasOne(student => student.Department)
            .WithMany(department => department.Students)
            .HasForeignKey(student => student.DepartmentID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CourseInstructor>()
            .HasKey(courseInstructor => new
            {
                courseInstructor.CourseID,
                courseInstructor.InstructorID
            });

        modelBuilder.Entity<CourseInstructor>()
            .HasOne(courseInstructor => courseInstructor.Course)
            .WithMany(course => course.CourseInstructors)
            .HasForeignKey(courseInstructor => courseInstructor.CourseID);

        modelBuilder.Entity<CourseInstructor>()
            .HasOne(courseInstructor => courseInstructor.Instructor)
            .WithMany(instructor => instructor.CourseInstructors)
            .HasForeignKey(courseInstructor => courseInstructor.InstructorID);
    }
}