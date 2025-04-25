using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class Administration : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public Administration(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public IActionResult RegisterStudent()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStudent(Student model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (await _context.Students.AnyAsync(s => s.Email == model.Email))
            {
                ModelState.AddModelError("", "A student with this email already exists.");
                return View(model);
            }

            // Enforce UTC time for CreatedAt if needed
            model.CreatedAt = DateTime.UtcNow;

            await _context.Students.AddAsync(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student registered successfully!";
            return RedirectToAction("Login", "Accounts"); // Redirect to login or student list page
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while registering the student.");
            Console.WriteLine($"Student Registration Error: {ex.Message}");
            return View(model);
        }
    }
}
