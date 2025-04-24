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

        public async Task<IActionResult> ViewRequests()
        {
            var requests = await _context.Requests
                .Where(r => !r.IsAccepted) 
                .Include(r => r.User)
                .Include(r => r.VideoFile)
                .Include(r => r.AudioFile)
                .Include(r => r.PdfFile)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(Guid requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.IsAccepted = true;

            // Assign the requested file to the user
            _context.AssignUsers.Add(new AssignUser
            {
                UserId = request.UserId,
                VideoId = request.VideoId,
                AudioId = request.AudioId,
                PdfId = request.PdfId,
                AssignedDate = DateTime.UtcNow,
                Year = DateTime.UtcNow.Year
            });


            request.IsAccepted = true;

            await _context.SaveChangesAsync();
            return RedirectToAction("ViewRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequest(Guid requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("ViewRequests");
        }

    }
}
