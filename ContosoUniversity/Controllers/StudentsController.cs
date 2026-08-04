using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers;

public class StudentsController : Controller
{
    private readonly SchoolContext _context;

    public StudentsController(SchoolContext context)
    {
        _context = context;
    }

    // GET: Students
    public async Task<IActionResult> Index()
    {
        List<Student> students = await _context.Students
            .AsNoTracking()
            .Include(student => student.Enrollments)
            .ThenInclude(enrollment => enrollment.Course)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstMidName)
            .ToListAsync();

        // Used by the course cards inside the Edit Student modal.
        ViewBag.AllCourses = await _context.Courses
            .AsNoTracking()
            .Include(course => course.Enrollments)
            .OrderBy(course => course.Title)
            .ToListAsync();

        return View(students);
    }

    // GET: Students/Create
    public async Task<IActionResult> Create()
    {
        CreateStudentViewModel model = new()
        {
            EnrollmentDate = DateTime.Today,
            Courses = await GetCourseSelectionsAsync()
        };

        return View(model);
    }

    // POST: Students/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateStudentViewModel model)
    {
        model.SelectedCourseIds ??= new List<int>();

        List<int> selectedCourseIds = model.SelectedCourseIds
            .Distinct()
            .ToList();

        if (selectedCourseIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.SelectedCourseIds),
                "Select at least one course.");
        }

        List<int> validCourseIds = new();

        if (selectedCourseIds.Count > 0)
        {
            validCourseIds = await _context.Courses
                .Where(course =>
                    selectedCourseIds.Contains(course.CourseID))
                .Select(course => course.CourseID)
                .ToListAsync();

            if (validCourseIds.Count != selectedCourseIds.Count)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedCourseIds),
                    "One or more selected courses are invalid.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Courses = await GetCourseSelectionsAsync();
            return View(model);
        }

        Student student = new()
        {
            FirstMidName = model.FirstMidName.Trim(),
            LastName = model.LastName.Trim(),
            EnrollmentDate = model.EnrollmentDate!.Value
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        List<Enrollment> enrollments = validCourseIds
            .Select(courseId => new Enrollment
            {
                StudentID = student.ID,
                CourseID = courseId,
                Grade = null
            })
            .ToList();

        _context.Enrollments.AddRange(enrollments);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The student was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Students/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("ID,LastName,FirstMidName,EnrollmentDate")]
        Student formStudent,
        List<int>? selectedCourseIds,
        Dictionary<int, Grade?>? enrollmentGrades)
    {
        if (id != formStudent.ID)
        {
            return NotFound();
        }

        selectedCourseIds ??= new List<int>();
        enrollmentGrades ??= new Dictionary<int, Grade?>();

        List<int> distinctCourseIds = selectedCourseIds
            .Distinct()
            .ToList();

        if (distinctCourseIds.Count == 0)
        {
            TempData["ErrorMessage"] =
                "The student must be enrolled in at least one course.";

            return RedirectToAction(nameof(Index));
        }

        List<int> validCourseIds = await _context.Courses
            .Where(course =>
                distinctCourseIds.Contains(course.CourseID))
            .Select(course => course.CourseID)
            .ToListAsync();

        if (validCourseIds.Count != distinctCourseIds.Count)
        {
            TempData["ErrorMessage"] =
                "One or more selected courses are invalid.";

            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "The student information or grades are invalid.";

            return RedirectToAction(nameof(Index));
        }

        Student? student = await _context.Students
            .Include(existingStudent =>
                existingStudent.Enrollments)
            .FirstOrDefaultAsync(existingStudent =>
                existingStudent.ID == id);

        if (student is null)
        {
            return NotFound();
        }

        // Update the basic student information.
        student.FirstMidName =
            formStudent.FirstMidName.Trim();

        student.LastName =
            formStudent.LastName.Trim();

        student.EnrollmentDate =
            formStudent.EnrollmentDate;

        HashSet<int> selectedCourseSet =
            validCourseIds.ToHashSet();

        // Remove enrollment records for unchecked courses.
        List<Enrollment> enrollmentsToRemove =
            student.Enrollments
                .Where(enrollment =>
                    !selectedCourseSet.Contains(
                        enrollment.CourseID))
                .ToList();

        _context.Enrollments.RemoveRange(
            enrollmentsToRemove);

        // Update grades for current courses and add new courses.
        foreach (int courseId in selectedCourseSet)
        {
            Enrollment? existingEnrollment =
                student.Enrollments
                    .FirstOrDefault(enrollment =>
                        enrollment.CourseID == courseId);

            Grade? selectedGrade = null;

            if (enrollmentGrades.TryGetValue(
                    courseId,
                    out Grade? submittedGrade))
            {
                selectedGrade = submittedGrade;
            }

            if (existingEnrollment is not null)
            {
                // Existing enrollment: update its grade.
                existingEnrollment.Grade =
                    selectedGrade;
            }
            else
            {
                // Newly selected course: create an enrollment.
                Enrollment newEnrollment = new()
                {
                    StudentID = student.ID,
                    CourseID = courseId,
                    Grade = selectedGrade
                };

                _context.Enrollments.Add(
                    newEnrollment);
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The student, courses, and grades were updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Students/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Student? student = await _context.Students
            .Include(existingStudent =>
                existingStudent.Enrollments)
            .FirstOrDefaultAsync(existingStudent =>
                existingStudent.ID == id);

        if (student is null)
        {
            return NotFound();
        }

        _context.Enrollments.RemoveRange(
            student.Enrollments);

        _context.Students.Remove(student);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The student was deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<CourseSelectionViewModel>>
        GetCourseSelectionsAsync()
    {
        return await _context.Courses
            .AsNoTracking()
            .OrderBy(course => course.Title)
            .Select(course => new CourseSelectionViewModel
            {
                CourseID = course.CourseID,
                Title = course.Title,
                Credits = course.Credits,
                EnrolledStudentCount =
                    course.Enrollments.Count()
            })
            .ToListAsync();
    }
}