using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DRM.Data;
using DRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using DRM.Services;

namespace DRM.Controllers
{
    [Authorize]
    public class ManageContent : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileEncryptionService _fileEncryptionService;

        // Constructor with Dependency Injection for Encryption Service
        public ManageContent(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFileEncryptionService fileEncryptionService)
        {
            _context = context;
            _userManager = userManager;
            _fileEncryptionService = fileEncryptionService;
        }

        // ✅ View all content
        public async Task<IActionResult> ViewContent()
        {
            var audioFiles = await _context.AudioFiles.OrderByDescending(a => a.DateOfUpload).ToListAsync();
            var videoFiles = await _context.VideoFiles.OrderByDescending(v => v.DateOfUpload).ToListAsync();
            var pdfFiles = await _context.PdfFiles.OrderByDescending(p => p.DateOfUpload).ToListAsync();

            ViewBag.AudioFiles = audioFiles.Select((a, index) => new { SN = index + 1, a.Id, a.Name, a.Category, a.DateOfUpload, a.Lock }).ToList();
            ViewBag.VideoFiles = videoFiles.Select((v, index) => new { SN = index + 1, v.Id, v.Name, v.Category, v.DateOfUpload, v.Lock }).ToList();
            ViewBag.PdfFiles = pdfFiles.Select((p, index) => new { SN = index + 1, p.Id, p.Name, p.Category, p.DateOfUpload, p.Lock }).ToList();

            return View();
        }

        public IActionResult AddContent()
        {
            return View();
        }

        // ✅ Upload Audio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(string name, string category, IFormFile file)
        {
            if (!IsValidFile(file, "audio/mpeg"))
                return BadRequest("Invalid audio file.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not authenticated.");

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Create a new PdfFile entity and save it to the database
            var audioFile = new AudioFile
            {
                Name = file.FileName,
                Category = category,
                EncryptedContent = fileBytes, // Store the raw file content without encryption
                DateOfUpload = DateTime.UtcNow,
                UploadedBy = user.UserName
            };


            _context.AudioFiles.Add(audioFile);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewContent");
        }

        // ✅ Upload Video
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVideo(string name, string category, IFormFile file)
        {
            if (!IsValidFile(file, "video/mp4"))
                return BadRequest("Invalid video file.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not authenticated.");

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Create a new PdfFile entity and save it to the database
            var videoFile = new VideoFile
            {
                Name = file.FileName,
                Category = category,
                EncryptedContent = fileBytes, // Store the raw file content without encryption
                DateOfUpload = DateTime.UtcNow,
                UploadedBy = user.UserName
            };


            _context.VideoFiles.Add(videoFile);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewContent");
        }

        // ✅ Upload PDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPdf(string name, string category, IFormFile file)
        {
            // Validate the file type
            if (!IsValidFile(file, "application/pdf"))
                return BadRequest("Invalid PDF file.");

            // Get the authenticated user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not authenticated.");

            // Read the file bytes
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Create a new PdfFile entity and save it to the database
            var pdfFile = new PdfFile
            {
                Name = file.FileName,
                Category = category,
                EncryptedContent = fileBytes, // Store the raw file content without encryption
                DateOfUpload = DateTime.UtcNow,
                UploadedBy = user.UserName
            };

            // Add the file to the database and save changes
            _context.PdfFiles.Add(pdfFile);
            await _context.SaveChangesAsync();

            // Redirect to the "ViewContent" action after the upload
            return RedirectToAction("ViewContent");
        }


        // ✅ Lock a File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockFile(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
                return BadRequest("Invalid file type or ID.");

            if (!Guid.TryParse(id, out Guid fileId))
                return BadRequest("Invalid file ID format.");

            var file = await GetFileById(fileId, type);
            if (file == null) return NotFound("File not found.");

            file.Lock = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewContent");
        }

        // ✅ Unlock a File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFile(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
                return BadRequest("Invalid file type or ID.");

            if (!Guid.TryParse(id, out Guid fileId))
                return BadRequest("Invalid file ID format.");

            var file = await GetFileById(fileId, type);
            if (file == null) return NotFound("File not found.");

            file.Lock = false;
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewContent");
        }

        // ✅ Delete a File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
                return BadRequest("Invalid file type or ID.");

            if (!Guid.TryParse(id, out Guid fileId))
                return BadRequest("Invalid file ID format.");

            var file = await GetFileById(fileId, type);
            if (file == null) return NotFound("File not found.");

            _context.Remove(file);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewContent");
        }

        // ✅ Helper Method: Get File by ID and Type
        private async Task<dynamic> GetFileById(Guid id, string type)
        {
            return type switch
            {
                "audio" => await _context.AudioFiles.FindAsync(id),
                "video" => await _context.VideoFiles.FindAsync(id),
                "pdf" => await _context.PdfFiles.FindAsync(id),
                _ => null
            };
        }

        // ✅ Reads file bytes
        private async Task<byte[]> GetFileBytes(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        // ✅ Validates file type before processing
        private bool IsValidFile(IFormFile file, string expectedMimeType)
        {
            return file != null && file.Length > 0 && file.ContentType == expectedMimeType;
        }
    }
}
