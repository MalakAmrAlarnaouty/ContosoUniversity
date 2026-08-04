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
            .Include(student => student.Department)
            .Include(student => student.Enrollments)
                .ThenInclude(enrollment => enrollment.Course)
                    .ThenInclude(course => course.Department)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstMidName)
            .ToListAsync();

        // Used by the course cards inside the Edit Student modal.
        ViewBag.AllCourses = await _context.Courses
            .AsNoTracking()
            .Include(course => course.Department)
            .Include(course => course.Enrollments)
            .OrderBy(course => course.Title)
            .ToListAsync();

        // Used by the department dropdown inside the Edit Student modal.
        ViewBag.AllDepartments = await _context.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync();

        return View(students);
    }

    // GET: Students/Create
    public async Task<IActionResult> Create()
    {
        CreateStudentViewModel model = new()
        {
            EnrollmentDate = DateTime.Today,
            Departments = await GetDepartmentSelectionsAsync(),
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
        NormalizeCreateModel(model);

        List<int> selectedCourseIds =
            model.SelectedCourseIds
                .Distinct()
                .ToList();

        bool departmentExists =
            await ValidateCreateDepartmentAsync(model);

        if (selectedCourseIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.SelectedCourseIds),
                "Select at least one course.");
        }

        List<int> validCourseIds =
            await ValidateCreateCoursesAsync(
                model,
                selectedCourseIds,
                departmentExists);

        if (!ModelState.IsValid)
        {
            model.Departments =
                await GetDepartmentSelectionsAsync();

            model.Courses =
                await GetCourseSelectionsAsync();

            return View(model);
        }

        Student student = new()
        {
            FirstMidName = model.FirstMidName,
            LastName = model.LastName,
            EnrollmentDate = model.EnrollmentDate!.Value,
            DepartmentID = model.DepartmentID
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        List<Enrollment> enrollments =
            validCourseIds
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
            $"The student \"{student.FullName}\" was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Students/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "ID,LastName,FirstMidName,EnrollmentDate,DepartmentID")]
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

        formStudent.FirstMidName =
            formStudent.FirstMidName?.Trim()
            ?? string.Empty;

        formStudent.LastName =
            formStudent.LastName?.Trim()
            ?? string.Empty;

        ValidateStudentNames(formStudent);

        if (!formStudent.DepartmentID.HasValue)
        {
            ModelState.AddModelError(
                nameof(formStudent.DepartmentID),
                "Select a faculty or department.");
        }

        bool departmentExists = false;

        if (formStudent.DepartmentID.HasValue)
        {
            departmentExists =
                await _context.Departments
                    .AnyAsync(department =>
                        department.DepartmentID ==
                        formStudent.DepartmentID.Value);

            if (!departmentExists)
            {
                ModelState.AddModelError(
                    nameof(formStudent.DepartmentID),
                    "The selected department does not exist.");
            }
        }

        List<int> distinctCourseIds =
            selectedCourseIds
                .Distinct()
                .ToList();

        if (distinctCourseIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(selectedCourseIds),
                "The student must be enrolled in at least one course.");
        }

        List<int> validCourseIds = new();

        if (departmentExists &&
            formStudent.DepartmentID.HasValue &&
            distinctCourseIds.Count > 0)
        {
            validCourseIds = await _context.Courses
                .Where(course =>
                    distinctCourseIds.Contains(
                        course.CourseID) &&
                    course.DepartmentID ==
                    formStudent.DepartmentID.Value)
                .Select(course => course.CourseID)
                .ToListAsync();

            if (validCourseIds.Count !=
                distinctCourseIds.Count)
            {
                ModelState.AddModelError(
                    nameof(selectedCourseIds),
                    "The student can only be enrolled in courses from the selected department.");
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                GetFirstModelError(
                    "The student information is invalid.");

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

        student.FirstMidName =
            formStudent.FirstMidName;

        student.LastName =
            formStudent.LastName;

        student.EnrollmentDate =
            formStudent.EnrollmentDate;

        student.DepartmentID =
            formStudent.DepartmentID;

        HashSet<int> selectedCourseSet =
            validCourseIds.ToHashSet();

        // Remove courses that are no longer selected.
        List<Enrollment> enrollmentsToRemove =
            student.Enrollments
                .Where(enrollment =>
                    !selectedCourseSet.Contains(
                        enrollment.CourseID))
                .ToList();

        _context.Enrollments.RemoveRange(
            enrollmentsToRemove);

        // Add new courses and update grades.
        foreach (int courseId in selectedCourseSet)
        {
            Enrollment? existingEnrollment =
                student.Enrollments
                    .FirstOrDefault(enrollment =>
                        enrollment.CourseID ==
                        courseId);

            Grade? selectedGrade = null;

            if (enrollmentGrades.TryGetValue(
                    courseId,
                    out Grade? submittedGrade))
            {
                selectedGrade = submittedGrade;
            }

            if (existingEnrollment is not null)
            {
                existingEnrollment.Grade =
                    selectedGrade;
            }
            else
            {
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
            "The student, department, courses, and grades were updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Students/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
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
            $"The student \"{student.FullName}\" was deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeCreateModel(
        CreateStudentViewModel model)
    {
        model.FirstMidName =
            model.FirstMidName?.Trim()
            ?? string.Empty;

        model.LastName =
            model.LastName?.Trim()
            ?? string.Empty;

        model.SelectedCourseIds ??=
            new List<int>();
    }

    private async Task<bool>
        ValidateCreateDepartmentAsync(
            CreateStudentViewModel model)
    {
        if (!model.DepartmentID.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.DepartmentID),
                "Select a faculty or department.");

            return false;
        }

        bool departmentExists =
            await _context.Departments
                .AnyAsync(department =>
                    department.DepartmentID ==
                    model.DepartmentID.Value);

        if (!departmentExists)
        {
            ModelState.AddModelError(
                nameof(model.DepartmentID),
                "The selected department does not exist.");
        }

        return departmentExists;
    }

    private async Task<List<int>>
        ValidateCreateCoursesAsync(
            CreateStudentViewModel model,
            List<int> selectedCourseIds,
            bool departmentExists)
    {
        if (!departmentExists ||
            !model.DepartmentID.HasValue ||
            selectedCourseIds.Count == 0)
        {
            return new List<int>();
        }

        List<int> validCourseIds =
            await _context.Courses
                .Where(course =>
                    selectedCourseIds.Contains(
                        course.CourseID) &&
                    course.DepartmentID ==
                    model.DepartmentID.Value)
                .Select(course =>
                    course.CourseID)
                .ToListAsync();

        if (validCourseIds.Count !=
            selectedCourseIds.Count)
        {
            ModelState.AddModelError(
                nameof(model.SelectedCourseIds),
                "You can only select courses assigned to the selected department.");
        }

        return validCourseIds;
    }

    private void ValidateStudentNames(
        Student student)
    {
        if (string.IsNullOrWhiteSpace(
                student.FirstMidName))
        {
            ModelState.AddModelError(
                nameof(student.FirstMidName),
                "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                student.LastName))
        {
            ModelState.AddModelError(
                nameof(student.LastName),
                "Last name is required.");
        }
    }

    private string GetFirstModelError(
        string defaultMessage)
    {
        string? firstError =
            ModelState.Values
                .SelectMany(value =>
                    value.Errors)
                .Select(error =>
                    error.ErrorMessage)
                .FirstOrDefault(message =>
                    !string.IsNullOrWhiteSpace(
                        message));

        return firstError ?? defaultMessage;
    }

    private async Task<List<CourseSelectionViewModel>>
        GetCourseSelectionsAsync()
    {
        return await _context.Courses
            .AsNoTracking()
            .OrderBy(course =>
                course.Title)
            .Select(course =>
                new CourseSelectionViewModel
                {
                    CourseID =
                        course.CourseID,

                    Title =
                        course.Title,

                    Credits =
                        course.Credits,

                    DepartmentID =
                        course.DepartmentID,

                    DepartmentName =
                        course.Department != null
                            ? course.Department.Name
                            : "No department",

                    EnrolledStudentCount =
                        course.Enrollments.Count()
                })
            .ToListAsync();
    }

    private async Task<
        List<DepartmentSelectionViewModel>>
        GetDepartmentSelectionsAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(department =>
                department.Name)
            .Select(department =>
                new DepartmentSelectionViewModel
                {
                    DepartmentID =
                        department.DepartmentID,

                    Name =
                        department.Name
                })
            .ToListAsync();
    }
}