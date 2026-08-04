using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.ViewModels;

public class CreateStudentViewModel
{
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstMidName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enrollment date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Enrollment Date")]
    public DateTime? EnrollmentDate { get; set; }

    [Required(ErrorMessage = "Select a faculty or department.")]
    [Display(Name = "Faculty / Department")]
    public int? DepartmentID { get; set; }

    public List<int> SelectedCourseIds { get; set; } = new();

    public List<DepartmentSelectionViewModel> Departments { get; set; }
        = new();

    public List<CourseSelectionViewModel> Courses { get; set; }
        = new();
}

public class DepartmentSelectionViewModel
{
    public int DepartmentID { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CourseSelectionViewModel
{
    public int CourseID { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Credits { get; set; }

    public int? DepartmentID { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public int EnrolledStudentCount { get; set; }
}