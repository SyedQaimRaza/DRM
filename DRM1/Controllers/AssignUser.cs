using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DRM.Data;
using DRM.Models;

namespace DRM.Controllers
{
    [Authorize]
    public class AssignUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ View Users & Available Content
        public async Task<IActionResult> AssignContent()
        {
            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.Videos = await _context.VideoFiles.ToListAsync();
            ViewBag.Audios = await _context.AudioFiles.ToListAsync();
            ViewBag.Pdfs = await _context.PdfFiles.ToListAsync();

            return View();
        }

        // ✅ Assign Content to User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignContent(string userId, Guid[] videoId, Guid[] pdfId, Guid[] audioId, DateTime assignedDate)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("Invalid user ID.");

            // ✅ Assign each selected content separately
            foreach (var vid in videoId)
            {
                _context.AssignUsers.Add(new AssignUser
                {
                    UserId = userId,
                    VideoId = vid,
                    AssignedDate = assignedDate,
                    Year = assignedDate.Year
                });
            }

            foreach (var aud in audioId)
            {
                _context.AssignUsers.Add(new AssignUser
                {
                    UserId = userId,
                    AudioId = aud,
                    AssignedDate = assignedDate,
                    Year = assignedDate.Year
                });
            }

            foreach (var pdf in pdfId)
            {
                _context.AssignUsers.Add(new AssignUser
                {
                    UserId = userId,
                    PdfId = pdf,
                    AssignedDate = assignedDate,
                    Year = assignedDate.Year
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("AssignContent");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserAssignedContent(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("Invalid user ID.");

            var assignedContent = await _context.AssignUsers
                .Where(a => a.UserId == userId)
                .Include(a => a.VideoFile)
                .Include(a => a.AudioFile)
                .Include(a => a.PdfFile)
                .Select(a => new
                {
                    a.Id,
                    Name = a.VideoFile != null ? a.VideoFile.Name :
                           a.AudioFile != null ? a.AudioFile.Name :
                           a.PdfFile != null ? a.PdfFile.Name : "Unknown",
                    Category = a.VideoFile != null ? a.VideoFile.Category :
                               a.AudioFile != null ? a.AudioFile.Category :
                               a.PdfFile != null ? a.PdfFile.Category : "Unknown"
                })
                .ToListAsync();

            ViewBag.AssignedContent = assignedContent;
            ViewBag.UserId = userId;

            return View("UserAssignedContent");
        }

        // ✅ Delete Assigned Content for a User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignedContent(Guid assignId)
        {
            var assignRecord = await _context.AssignUsers.FindAsync(assignId);
            if (assignRecord == null) return NotFound();

            _context.AssignUsers.Remove(assignRecord);
            await _context.SaveChangesAsync();

            return RedirectToAction("UserAssignedContent", new { userId = assignRecord.UserId });
        }
    }
}
