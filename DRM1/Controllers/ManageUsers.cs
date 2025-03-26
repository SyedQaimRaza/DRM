using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DRM.Models;
using DRM.Data;
using Microsoft.AspNetCore.Hosting;

namespace DRM.Controllers
{
    [Authorize] // Ensure all actions require authentication
    public class ManageUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IWebHostEnvironment _webHostEnvironment;
        public ManageUsersController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }



        [Authorize]
        public async Task<IActionResult> ManageUsers()
        {
            var usersList = _userManager.Users.ToList(); // Fetch users first

            var users = new List<object>(); // Create a list to store users

            foreach (var (user, index) in usersList.Select((value, i) => (value, i)))
            {
                var roles = await _userManager.GetRolesAsync(user); // Fetch roles asynchronously

                users.Add(new
                {
                    SN = index + 1,
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Designation,
                    Roles = string.Join(", ", roles), // Store roles as a comma-separated string
                    LockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow,
                    ProfileImage = !string.IsNullOrEmpty(user.ProfileImage) ? "/" + user.ProfileImage.Replace("\\", "/") : "/images/default-profile.png"
                });
            }

            ViewBag.Users = users;

            return View();
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken] // Prevent CSRF Attacks
        public async Task<IActionResult> LockUser([FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("Invalid user ID.");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            user.LockoutEnd = DateTime.UtcNow.AddYears(100); // Lock indefinitely
            await _userManager.UpdateAsync(user);

            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser([FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("Invalid user ID.");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            user.LockoutEnd = null; // Unlock user
            await _userManager.UpdateAsync(user);

            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser([FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("Invalid user ID.");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            await _userManager.DeleteAsync(user);

            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "Invalid user ID." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user); // Fetch roles

            return Json(new
            {
                Id = user.Id,
                Name = user.Name ?? "",
                Email = user.Email ?? "",
                Designation = user.Designation ?? "N/A",
                Roles = roles.Count > 0 ? string.Join(", ", roles) : "No Role",
                LockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow,
                ProfileImage = !string.IsNullOrEmpty(user.ProfileImage) ? "/" + user.ProfileImage.Replace("\\", "/") : "/images/default-profile.png"
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(string userId, string name, string designation, IFormFile profileImage)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(designation))
            {
                TempData["Error"] = "Invalid input data.";
                return RedirectToAction("ManageUsers");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            user.Name = name.Trim();
            user.Designation = designation.Trim();

            // Handle Profile Image Upload
            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                string filePath = Path.Combine("uploads", uniqueFileName);
                string serverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);

                // Save new image
                using (var stream = new FileStream(serverFilePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfileImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                user.ProfileImage = filePath; // Store new image path in database
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user.";
            }

            return RedirectToAction("ManageUsers");
        }

    }
}
