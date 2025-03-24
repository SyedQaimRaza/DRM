using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DRM.Data;
using DRM.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DRM.Controllers
{
    [Authorize]
    public class ManageContent : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManageContent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            var audioFile = new AudioFile
            {
                Name = name,
                Category = category,
                EncryptedContent = EncryptFile(await GetFileBytes(file)),
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

            var videoFile = new VideoFile
            {
                Name = name,
                Category = category,
                EncryptedContent = EncryptFile(await GetFileBytes(file)),
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
            if (!IsValidFile(file, "application/pdf"))
                return BadRequest("Invalid PDF file.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not authenticated.");

            var pdfFile = new PdfFile
            {
                Name = name,
                Category = category,
                EncryptedContent = EncryptFile(await GetFileBytes(file)),
                DateOfUpload = DateTime.UtcNow,
                UploadedBy = user.UserName
            };

            _context.PdfFiles.Add(pdfFile);
            await _context.SaveChangesAsync();
            return RedirectToAction("ViewContent");
        }

        // ✅ Lock a File (Fix: Use `Guid` instead of `int`)
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

        // ✅ Unlock a File (Fix: Use `Guid`)
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

        // ✅ Delete a File (Fix: Use `Guid`)
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

        // ✅ Helper Method: Get File by ID and Type (Fix: Use `Guid`)
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
        // ✅ Helper Method: Get File by ID and Type
        private async Task<dynamic> GetFileById(int id, string type)
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

        // ✅ Encrypts file before saving
        private byte[] EncryptFile(byte[] data)
        {
            using Aes aes = Aes.Create();
            aes.Key = GenerateEncryptionKey();
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        // ✅ Decrypts file before serving it to the user
        private byte[] DecryptFile(byte[] encryptedData)
        {
            using Aes aes = Aes.Create();
            aes.Key = GenerateEncryptionKey();
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        // ✅ Generates Encryption Key
        private byte[] GenerateEncryptionKey()
        {
            string key = "hr803349@gmail.com";
            return Encoding.UTF8.GetBytes(key.PadRight(32, 'X').Substring(0, 32)); // Ensures exactly 32 bytes
        }

        // ✅ Validates file type before processing
        private bool IsValidFile(IFormFile file, string expectedMimeType)
        {
            return file != null && file.Length > 0 && file.ContentType == expectedMimeType;
        }
    }
}
