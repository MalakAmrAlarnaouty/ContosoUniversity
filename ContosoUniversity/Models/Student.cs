using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Models
{
    public class Student
    {
        public int ID { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstMidName { get; set; } = string.Empty;

        [Display(Name = "Enrollment Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(
            DataFormatString = "{0:MM/dd/yyyy}",
            ApplyFormatInEditMode = true)]
        public DateTime EnrollmentDate { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}