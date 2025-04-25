using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace DRM.Controllers
{
    [Authorize]
    public class ManageStudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ManageStudentsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> ManageStudent()
        {
            var students = await _context.Students
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Students = students;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent([FromForm] Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound("Student not found.");

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageStudent");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetStudentDetails(Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found." });

            return Json(student);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStudent(Guid studentId, string fullName, string grade, DateTime? dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(grade))
            {
                TempData["Error"] = "Invalid input data.";
                return RedirectToAction("ManageStudent");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction("ManageStudent");
            }

            student.FullName = fullName.Trim();
            student.Grade = grade.Trim();
            if (dateOfBirth.HasValue)
            {
                student.DateOfBirth = dateOfBirth;
            }

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction("ManageStudent");
        }
    }
}
