using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers;

public class InstructorsController : Controller
{
    private readonly SchoolContext _context;

    public InstructorsController(SchoolContext context)
    {
        _context = context;
    }

    // GET: Instructors
    public async Task<IActionResult> Index()
    {
        List<Instructor> instructors =
            await _context.Instructors
                .AsNoTracking()
                .Include(instructor =>
                    instructor.Department)
                .Include(instructor =>
                    instructor.CourseInstructors)
                    .ThenInclude(courseInstructor =>
                        courseInstructor.Course)
                .OrderBy(instructor =>
                    instructor.LastName)
                .ThenBy(instructor =>
                    instructor.FirstMidName)
                .ToListAsync();

        ViewBag.TotalInstructors =
            instructors.Count;

        ViewBag.TotalDepartments =
            await _context.Departments
                .AsNoTracking()
                .CountAsync();

        return View(instructors);
    }

    // GET: Instructors/Create
    public async Task<IActionResult> Create()
    {
        InstructorFormViewModel model = new()
        {
            HireDate = DateTime.Today
        };

        await LoadFormOptionsAsync(model);

        return View(model);
    }

    // POST: Instructors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        InstructorFormViewModel model)
    {
        NormalizeModel(model);

        List<int> validCourseIds =
            await ValidateSelectionsAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync(model);

            return View(model);
        }

        Instructor instructor = new()
        {
            FirstMidName = model.FirstMidName,
            LastName = model.LastName,
            HireDate = model.HireDate!.Value,
            OfficeLocation = model.OfficeLocation,
            DepartmentID = model.DepartmentID
        };

        _context.Instructors.Add(instructor);

        await _context.SaveChangesAsync();

        List<CourseInstructor> assignments =
            validCourseIds
                .Select(courseId =>
                    new CourseInstructor
                    {
                        InstructorID =
                            instructor.InstructorID,

                        CourseID =
                            courseId
                    })
                .ToList();

        _context.CourseInstructors.AddRange(
            assignments);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Instructor \"{instructor.FullName}\" was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Instructors/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        Instructor? instructor =
            await _context.Instructors
                .AsNoTracking()
                .Include(existingInstructor =>
                    existingInstructor.CourseInstructors)
                .FirstOrDefaultAsync(existingInstructor =>
                    existingInstructor.InstructorID == id);

        if (instructor is null)
        {
            return NotFound();
        }

        InstructorFormViewModel model = new()
        {
            InstructorID =
                instructor.InstructorID,

            FirstMidName =
                instructor.FirstMidName,

            LastName =
                instructor.LastName,

            HireDate =
                instructor.HireDate,

            OfficeLocation =
                instructor.OfficeLocation,

            DepartmentID =
                instructor.DepartmentID,

            SelectedCourseIds =
                instructor.CourseInstructors
                    .Select(courseInstructor =>
                        courseInstructor.CourseID)
                    .ToList()
        };

        await LoadFormOptionsAsync(model);

        return View(model);
    }

    // POST: Instructors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        InstructorFormViewModel model)
    {
        if (id != model.InstructorID)
        {
            return NotFound();
        }

        NormalizeModel(model);

        List<int> validCourseIds =
            await ValidateSelectionsAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync(model);

            return View(model);
        }

        Instructor? instructor =
            await _context.Instructors
                .Include(existingInstructor =>
                    existingInstructor.CourseInstructors)
                .FirstOrDefaultAsync(existingInstructor =>
                    existingInstructor.InstructorID == id);

        if (instructor is null)
        {
            return NotFound();
        }

        instructor.FirstMidName =
            model.FirstMidName;

        instructor.LastName =
            model.LastName;

        instructor.HireDate =
            model.HireDate!.Value;

        instructor.OfficeLocation =
            model.OfficeLocation;

        instructor.DepartmentID =
            model.DepartmentID;

        HashSet<int> selectedCourseSet =
            validCourseIds.ToHashSet();

        List<CourseInstructor> assignmentsToRemove =
            instructor.CourseInstructors
                .Where(courseInstructor =>
                    !selectedCourseSet.Contains(
                        courseInstructor.CourseID))
                .ToList();

        _context.CourseInstructors.RemoveRange(
            assignmentsToRemove);

        HashSet<int> existingCourseIds =
            instructor.CourseInstructors
                .Select(courseInstructor =>
                    courseInstructor.CourseID)
                .ToHashSet();

        foreach (int courseId in selectedCourseSet)
        {
            if (!existingCourseIds.Contains(courseId))
            {
                CourseInstructor assignment = new()
                {
                    InstructorID =
                        instructor.InstructorID,

                    CourseID =
                        courseId
                };

                _context.CourseInstructors.Add(
                    assignment);
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Instructor \"{instructor.FullName}\" was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Instructors/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        Instructor? instructor =
            await _context.Instructors
                .Include(existingInstructor =>
                    existingInstructor.CourseInstructors)
                .FirstOrDefaultAsync(existingInstructor =>
                    existingInstructor.InstructorID == id);

        if (instructor is null)
        {
            return NotFound();
        }

        _context.CourseInstructors.RemoveRange(
            instructor.CourseInstructors);

        _context.Instructors.Remove(instructor);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Instructor \"{instructor.FullName}\" was deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeModel(
        InstructorFormViewModel model)
    {
        model.FirstMidName =
            model.FirstMidName?.Trim()
            ?? string.Empty;

        model.LastName =
            model.LastName?.Trim()
            ?? string.Empty;

        model.OfficeLocation =
            string.IsNullOrWhiteSpace(
                model.OfficeLocation)
                ? null
                : model.OfficeLocation.Trim();

        model.SelectedCourseIds ??=
            new List<int>();
    }

    private async Task<List<int>>
        ValidateSelectionsAsync(
            InstructorFormViewModel model)
    {
        bool departmentExists = false;

        if (!model.DepartmentID.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.DepartmentID),
                "Select a department.");
        }
        else
        {
            departmentExists =
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
        }

        List<int> selectedCourseIds =
            model.SelectedCourseIds
                .Distinct()
                .ToList();

        if (selectedCourseIds.Count == 0)
        {
            return new List<int>();
        }

        if (!departmentExists ||
            !model.DepartmentID.HasValue)
        {
            return new List<int>();
        }

        // Only accept courses assigned to the
        // selected instructor department.
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
                "The instructor can only teach courses assigned to the selected department.");
        }

        return validCourseIds;
    }

    private async Task LoadFormOptionsAsync(
        InstructorFormViewModel model)
    {
        model.Departments =
            await _context.Departments
                .AsNoTracking()
                .OrderBy(department =>
                    department.Name)
                .ToListAsync();

        model.Courses =
            await _context.Courses
                .AsNoTracking()
                .Include(course =>
                    course.Department)
                .OrderBy(course =>
                    course.Title)
                .ToListAsync();
    }
}