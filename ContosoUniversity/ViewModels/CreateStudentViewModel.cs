using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.ViewModels;

public class CreateStudentViewModel
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstMidName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Enrollment Date")]
    public DateTime? EnrollmentDate { get; set; }

    public List<int> SelectedCourseIds { get; set; }
        = new List<int>();

    public List<CourseSelectionViewModel> Courses { get; set; }
        = new List<CourseSelectionViewModel>();
}

public class CourseSelectionViewModel
{
    public int CourseID { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Credits { get; set; }

    public int EnrolledStudentCount { get; set; }
}