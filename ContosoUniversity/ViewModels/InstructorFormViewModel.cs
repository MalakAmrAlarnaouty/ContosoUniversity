using ContosoUniversity.Models;
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.ViewModels;

public class InstructorFormViewModel
{
    public int InstructorID { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstMidName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hire date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Hire Date")]
    public DateTime? HireDate { get; set; }

    [StringLength(100)]
    [Display(Name = "Office Location")]
    public string? OfficeLocation { get; set; }

    [Required(ErrorMessage = "Select a department.")]
    [Display(Name = "Department")]
    public int? DepartmentID { get; set; }

    public List<int> SelectedCourseIds { get; set; } = new();

    public List<Department> Departments { get; set; } = new();

    public List<Course> Courses { get; set; } = new();
}