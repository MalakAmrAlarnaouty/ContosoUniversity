using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult Create()
    {
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
        [Bind("CourseID,Title,Credits")]
        Course course)
    {
        course.Title = course.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(course.Title))
        {
            ModelState.AddModelError(
                nameof(course.Title),
                "Course title is required.");
        }

        bool courseIdExists = await _context.Courses
            .AnyAsync(existingCourse =>
                existingCourse.CourseID == course.CourseID);

        if (courseIdExists)
        {
            ModelState.AddModelError(
                nameof(course.CourseID),
                "A course with this ID already exists.");
        }

        if (!string.IsNullOrWhiteSpace(course.Title))
        {
            string normalizedTitle =
                course.Title.ToUpper();

            bool courseTitleExists = await _context.Courses
                .AnyAsync(existingCourse =>
                    existingCourse.Title.ToUpper()
                    == normalizedTitle);

            if (courseTitleExists)
            {
                ModelState.AddModelError(
                    nameof(course.Title),
                    "A course with this title already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(course);
        }

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The course \"{course.Title}\" was created successfully.";

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
}