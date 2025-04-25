using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DRM.MiddleWare;

public class Administration : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly AllowanceChecker _allowanceChecker;
    public Administration(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, AllowanceChecker allowanceChecker)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _allowanceChecker = allowanceChecker;
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
            if (!await _allowanceChecker.CanRegisterMoreStudentsAsync())
            {
                ModelState.AddModelError("", "Student limit reached. Cannot register more students.");
                return View(model);
            }

            if (await _context.Students.AnyAsync(s => s.Email == model.Email))
            {
                ModelState.AddModelError("", "A student with this email already exists.");
                return View(model);
            }

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
