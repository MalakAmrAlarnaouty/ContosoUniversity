using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers;

public class CoursesController : Controller
{
    private readonly SchoolContext _context;

    public CoursesController(SchoolContext context)
    {
        _context = context;
    }

    // GET: Courses
    public async Task<IActionResult> Index()
    {
        List<Course> courses = await _context.Courses
            .AsNoTracking()
            .Include(course => course.Department)
            .Include(course => course.Enrollments)
            .ThenInclude(enrollment => enrollment.Student)
            .OrderBy(course => course.Title)
            .ToListAsync();

        ViewBag.TotalCourses = courses.Count;

        ViewBag.TotalStudents = await _context.Enrollments
            .AsNoTracking()
            .Select(enrollment => enrollment.StudentID)
            .Distinct()
            .CountAsync();

        return View(courses);
    }

    // GET: Courses/Create
    public async Task<IActionResult> Create()
    {
        await LoadDepartmentsAsync();

        Course course = new()
        {
            Credits = 3
        };

        return View(course);
    }

    // POST: Courses/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CourseID,Title,Credits,DepartmentID")]
        Course course)
    {
        course.Title = course.Title?.Trim() ?? string.Empty;

        await ValidateCourseAsync(course, checkCourseId: true);

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync(course.DepartmentID);
            return View(course);
        }

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The course \"{course.Title}\" was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Courses/Edit/1050
    public async Task<IActionResult> Edit(int id)
    {
        Course? course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(course => course.CourseID == id);

        if (course is null)
        {
            return NotFound();
        }

        await LoadDepartmentsAsync(course.DepartmentID);

        return View(course);
    }

    // POST: Courses/Edit/1050
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("CourseID,Title,Credits,DepartmentID")]
        Course formCourse)
    {
        if (id != formCourse.CourseID)
        {
            return NotFound();
        }

        formCourse.Title =
            formCourse.Title?.Trim() ?? string.Empty;

        await ValidateCourseAsync(
            formCourse,
            checkCourseId: false);

        if (!ModelState.IsValid)
        {
            await LoadDepartmentsAsync(
                formCourse.DepartmentID);

            return View(formCourse);
        }

        Course? course = await _context.Courses
            .FirstOrDefaultAsync(existingCourse =>
                existingCourse.CourseID == id);

        if (course is null)
        {
            return NotFound();
        }

        course.Title = formCourse.Title;
        course.Credits = formCourse.Credits;
        course.DepartmentID = formCourse.DepartmentID;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The course \"{course.Title}\" was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Courses/Delete/1050
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        Course? course = await _context.Courses
            .Include(existingCourse =>
                existingCourse.Enrollments)
            .FirstOrDefaultAsync(existingCourse =>
                existingCourse.CourseID == id);

        if (course is null)
        {
            return NotFound();
        }

        if (course.Enrollments.Count > 0)
        {
            TempData["ErrorMessage"] =
                "This course cannot be deleted because students are enrolled in it.";

            return RedirectToAction(nameof(Index));
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The course \"{course.Title}\" was deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCourseAsync(
        Course course,
        bool checkCourseId)
    {
        if (string.IsNullOrWhiteSpace(course.Title))
        {
            ModelState.AddModelError(
                nameof(course.Title),
                "Course title is required.");
        }

        if (checkCourseId)
        {
            bool courseIdExists = await _context.Courses
                .AnyAsync(existingCourse =>
                    existingCourse.CourseID ==
                    course.CourseID);

            if (courseIdExists)
            {
                ModelState.AddModelError(
                    nameof(course.CourseID),
                    "A course with this ID already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(course.Title))
        {
            string normalizedTitle =
                course.Title.ToUpper();

            bool titleExists = await _context.Courses
                .AnyAsync(existingCourse =>
                    existingCourse.CourseID != course.CourseID &&
                    existingCourse.Title.ToUpper() ==
                    normalizedTitle);

            if (titleExists)
            {
                ModelState.AddModelError(
                    nameof(course.Title),
                    "Another course already uses this title.");
            }
        }

        if (course.DepartmentID.HasValue)
        {
            bool departmentExists =
                await _context.Departments
                    .AnyAsync(department =>
                        department.DepartmentID ==
                        course.DepartmentID.Value);

            if (!departmentExists)
            {
                ModelState.AddModelError(
                    nameof(course.DepartmentID),
                    "The selected department does not exist.");
            }
        }
    }

    private async Task LoadDepartmentsAsync(
        int? selectedDepartmentId = null)
    {
        List<Department> departments =
            await _context.Departments
                .AsNoTracking()
                .OrderBy(department =>
                    department.Name)
                .ToListAsync();

        ViewBag.Departments = new SelectList(
            departments,
            nameof(Department.DepartmentID),
            nameof(Department.Name),
            selectedDepartmentId);
    }
}