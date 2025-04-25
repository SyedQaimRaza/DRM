using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DRM.Models;
using DRM.Data;
using Microsoft.EntityFrameworkCore;

namespace DRM.Controllers
{
    [Authorize] // Ensure all actions require authentication
    public class DashboardsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        public DashboardsController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env, ApplicationDbContext context)
        {
            _userManager = userManager;
            _env = env;
            _context = context;
        }

        public async Task<IActionResult> Dashboard_4()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserName = user != null ? user.Name : "User";

            if (user != null && !string.IsNullOrEmpty(user.ProfileImage))
            {
                ViewBag.ProImg = "/" + user.ProfileImage.Replace("\\", "/"); // Ensure correct URL format
            }
            else
            {
                ViewBag.ProImg = "/images/default-profile.png"; // Default profile image
            }

            ViewBag.TotalUsers = _userManager.Users.Count();
            ViewBag.TotalUsers = _userManager.Users.Count();

            ViewBag.TotalVideoFiles = _context.VideoFiles.Count();
            ViewBag.TotalAudioFiles = _context.AudioFiles.Count();
            ViewBag.TotalPdfFiles = _context.PdfFiles.Count();

            string webRoot = _env.WebRootPath;

            var audioDir = Path.Combine(webRoot, "Content", "audio");
            var videoDir = Path.Combine(webRoot, "Content", "video");
            var pdfDir = Path.Combine(webRoot, "Content", "pdf");

            ViewBag.StaticAudioFiles = Directory.Exists(audioDir) ? Directory.GetFiles(audioDir).Length : 0;
            ViewBag.StaticVideoFiles = Directory.Exists(videoDir) ? Directory.GetFiles(videoDir).Length : 0;
            ViewBag.StaticPdfFiles = Directory.Exists(pdfDir) ? Directory.GetFiles(pdfDir).Length : 0;
            ViewBag.CurrentTime = DateTime.Now.ToString("hh:mm tt dd-MMM-yyyy");

           
            return View();
        }

       
        
    }
}
