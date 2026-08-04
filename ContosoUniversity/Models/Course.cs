using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models;

public class Course
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Display(Name = "Course ID")]
    [Range(
        1,
        99999,
        ErrorMessage = "Enter a valid course ID.")]
    public int CourseID { get; set; }

    [Required(ErrorMessage = "Course title is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "The title must be between 2 and 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Range(
        1,
        10,
        ErrorMessage = "Credits must be between 1 and 10.")]
    public int Credits { get; set; }

    // Nullable because existing courses may not have a department yet.
    [Display(Name = "Department")]
    public int? DepartmentID { get; set; }

    public Department? Department { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();
}