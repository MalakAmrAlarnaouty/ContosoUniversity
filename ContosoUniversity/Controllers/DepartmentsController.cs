using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers;

public class DepartmentsController : Controller
{
    private readonly SchoolContext _context;

    public DepartmentsController(SchoolContext context)
    {
        _context = context;
    }

    // GET: Departments
    public async Task<IActionResult> Index()
    {
        List<Department> departments = await _context.Departments
            .AsNoTracking()
            .Include(department => department.Courses)
            .Include(department => department.Instructors)
            .OrderBy(department => department.Name)
            .ToListAsync();

        ViewBag.TotalDepartments = departments.Count;

        ViewBag.TotalInstructors = await _context.Instructors
            .AsNoTracking()
            .CountAsync();

        return View(departments);
    }

    // GET: Departments/Create
    public IActionResult Create()
    {
        Department department = new()
        {
            StartDate = DateTime.Today,
            Budget = 0
        };

        return View(department);
    }

    // POST: Departments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,StartDate")] Department department)
    {
        department.Name =
            department.Name?.Trim() ?? string.Empty;

        // Budget is not used by this project.
        department.Budget = 0;

        if (string.IsNullOrWhiteSpace(department.Name))
        {
            ModelState.AddModelError(
                nameof(department.Name),
                "Department name is required.");
        }

        if (!string.IsNullOrWhiteSpace(department.Name))
        {
            string normalizedName =
                department.Name.ToUpper();

            bool nameExists = await _context.Departments
                .AnyAsync(existingDepartment =>
                    existingDepartment.Name.ToUpper() ==
                    normalizedName);

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(department.Name),
                    "A department with this name already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The department \"{department.Name}\" was created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Departments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("DepartmentID,Name,StartDate")]
        Department formDepartment)
    {
        if (id != formDepartment.DepartmentID)
        {
            return NotFound();
        }

        formDepartment.Name =
            formDepartment.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(formDepartment.Name))
        {
            TempData["ErrorMessage"] =
                "Department name is required.";

            return RedirectToAction(nameof(Index));
        }

        string normalizedName =
            formDepartment.Name.ToUpper();

        bool duplicateName = await _context.Departments
            .AnyAsync(department =>
                department.DepartmentID != id &&
                department.Name.ToUpper() ==
                normalizedName);

        if (duplicateName)
        {
            TempData["ErrorMessage"] =
                "Another department already uses this name.";

            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "The department information is invalid.";

            return RedirectToAction(nameof(Index));
        }

        Department? department =
            await _context.Departments.FindAsync(id);

        if (department is null)
        {
            return NotFound();
        }

        department.Name = formDepartment.Name;
        department.StartDate = formDepartment.StartDate;
        department.Budget = 0;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The department \"{department.Name}\" was updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Departments/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        Department? department = await _context.Departments
            .Include(existingDepartment =>
                existingDepartment.Courses)
            .Include(existingDepartment =>
                existingDepartment.Instructors)
            .FirstOrDefaultAsync(existingDepartment =>
                existingDepartment.DepartmentID == id);

        if (department is null)
        {
            return NotFound();
        }

        // Keep the courses, but remove their department assignment.
        foreach (Course course in department.Courses)
        {
            course.DepartmentID = null;
        }

        // Keep the instructors, but remove their department assignment.
        foreach (Instructor instructor in department.Instructors)
        {
            instructor.DepartmentID = null;
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"The department \"{department.Name}\" was deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}