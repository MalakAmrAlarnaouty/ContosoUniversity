using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Models;

public class Student
{
    public int ID { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstMidName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enrollment date is required.")]
    [Display(Name = "Enrollment Date")]
    [DataType(DataType.Date)]
    [DisplayFormat(
        DataFormatString = "{0:MM/dd/yyyy}",
        ApplyFormatInEditMode = true)]
    public DateTime EnrollmentDate { get; set; }

    [Display(Name = "Faculty / Department")]
    public int? DepartmentID { get; set; }

    public Department? Department { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();

    [Display(Name = "Full Name")]
    public string FullName => $"{FirstMidName} {LastName}";
}