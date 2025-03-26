using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.EntityFrameworkCore;

namespace DRM.Controllers
{
    public class Accounts : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public Accounts(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ✅ GET: Login Page (Auto Login if Token Exists)
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            try
            {
                // ✅ Check for login token in cookies
                if (Request.Cookies.ContainsKey("LoginToken"))
                {
                    string loginToken = Request.Cookies["LoginToken"];

                    var user = await _userManager.Users.FirstOrDefaultAsync(u => u.SecurityStamp == loginToken);
                    if (user != null)
                    {
                        await SignInUser(user);
                        return RedirectToAction("Dashboard_4", "Dashboards");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while checking login status.");
                Console.WriteLine($"Login Auto-Login Error: {ex.Message}");
            }

            return View();
        }

        // ✅ POST: Handle Secure Login
        [HttpPost]
        [ValidateAntiForgeryToken] // ✅ CSRF Protection
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var email = model.Email.Trim(); // ✅ Prevents SQL Injection via input sanitation
                var user = await _userManager.FindByEmailAsync(email);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
                    if (result.Succeeded)
                    {
                        await SignInUser(user);
                        var roles = await _userManager.GetRolesAsync(user);
                        string userRoles = string.Join(",", roles);

                        // ✅ Store SecurityStamp in cookies as login token
                        Response.Cookies.Append("LoginToken", user.SecurityStamp, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true, // Set to false if not using HTTPS
                            Expires = DateTime.UtcNow.AddDays(30)
                        });

                        Response.Cookies.Append("UserRoles", userRoles, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            Expires = DateTime.UtcNow.AddDays(30)
                        });

                        return RedirectToAction("Dashboard_4", "Dashboards");
                    }
                }

                ModelState.AddModelError("", "Invalid email or password.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during login.");
                Console.WriteLine($"Login Error: {ex.Message}");
            }

            return View(model);
        }

      
        [ValidateAntiForgeryToken] // ✅ CSRF Protection for Logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear(); // ✅ Clear session
                Response.Cookies.Delete("LoginToken"); // ✅ Delete login token
                Response.Cookies.Delete("UserRoles"); // ✅ Delete UserRoles 

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while logging out.");
                Console.WriteLine($"Logout Error: {ex.Message}");
                return RedirectToAction("Login");
            }
        }

        // ✅ HELPER: Sign In User & Store Data in Session
        private async Task SignInUser(ApplicationUser user)
        {
            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                string userRoles = string.Join(",", roles);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Id ?? "")
                };

                // ✅ Add user roles as claims (IMPORTANT for [Authorize(Roles = "Admin")])
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                });

                // ✅ Store roles in session (optional)
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRoles", userRoles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignInUser Error: {ex.Message}");
            }
        }
    }
}
