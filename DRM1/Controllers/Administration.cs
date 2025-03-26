using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DRM.Controllers
{
    [Authorize]
    public class Administration : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public Administration(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Designation))
                {
                    ModelState.AddModelError("", "Name, Email, and Designation are required.");
                    return View(model);
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "A user with this email already exists.");
                    return View(model);
                }

                string filePath = null;

                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
                    filePath = Path.Combine("uploads", uniqueFileName);

                    string serverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);
                    using (var stream = new FileStream(serverFilePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(stream);
                    }
                }

                // Generate UniqueKey using Email and Password
                string uniqueKey = EncryptUniqueKey($"{model.Email}{model.Password}");

                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    Name = model.Name.Trim(),
                    ProfileImage = filePath,
                    Designation = model.Designation.Trim(),
                    UniqueKey = uniqueKey
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    string role = model.Designation.Trim().ToLower() == "admin" ? "Admin" : "User";

                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    }

                    await _userManager.AddToRoleAsync(user, role);

                    return RedirectToAction("Login", "Accounts");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your request.");
                Console.WriteLine($"Error: {ex.Message}");
            }

            return View(model);
        }

        private static string EncryptUniqueKey(string plainText)
        {
            using var aes = Aes.Create();

            // Ensure the key is exactly 32 bytes long
            byte[] keyBytes = Encoding.UTF8.GetBytes("DevelopedByAnonymous".PadRight(32, 'X')); // Padding to 32 bytes
            aes.Key = keyBytes.Take(32).ToArray();
            aes.IV = new byte[16]; // 16-byte IV for AES-256

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }
    }
}
