using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DRM.Models;
using DRM.Data;

namespace DRM.Controllers
{
    [Authorize] // Ensure all actions require authentication
    public class DashboardsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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

            return View();
        }

       
        
    }
}
