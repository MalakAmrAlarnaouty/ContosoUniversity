using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models;

public class Department
{
    public int DepartmentID { get; set; }

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(
        50,
        MinimumLength = 2,
        ErrorMessage = "Department name must be between 2 and 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18,2)")]
    [Range(
        0,
        100000000,
        ErrorMessage = "Enter a valid department budget.")]
    public decimal Budget { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    public ICollection<Course> Courses { get; set; }
        = new List<Course>();
}