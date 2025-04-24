using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DRM.Models; 
using System.Threading.Tasks;
using System;
using DRM.Data;
using Microsoft.EntityFrameworkCore;

namespace DRM.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Route("api/[controller]")]
    public class SwaggerControllerApi : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        public SwaggerControllerApi(
          SignInManager<ApplicationUser> signInManager,
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }
        [HttpPost("student-login")]
        public async Task<IActionResult> StudentLogin([FromBody] Student model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid login data.");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == model.Email);

            if (student == null || student.Password != model.Password)
                return Unauthorized("Invalid email or password.");

            var rawToken = $"{student.FullName}:{student.Email}:{DateTime.UtcNow.Ticks}";
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawToken));

            return Ok(new
            {
                name = student.FullName,
                email = student.Email,
                token = token,
                sessionExpiresAt = DateTime.UtcNow.AddDays(30)
            });
        }


        [HttpGet("content-visualization")]
        public async Task<IActionResult> ContentVisualization()
        {
            var audioFiles = await _context.AudioFiles.OrderByDescending(a => a.DateOfUpload).ToListAsync();
            var videoFiles = await _context.VideoFiles.OrderByDescending(v => v.DateOfUpload).ToListAsync();
            var pdfFiles = await _context.PdfFiles.OrderByDescending(p => p.DateOfUpload).ToListAsync();

            var audioList = audioFiles.Select((a, index) => new
            {
                SN = index + 1,
                a.Id,
                a.Name,
                a.Category,
                a.DateOfUpload,
                a.Lock
            }).ToList();

            var videoList = videoFiles.Select((v, index) => new
            {
                SN = index + 1,
                v.Id,
                v.Name,
                v.Category,
                v.DateOfUpload,
                v.Lock
            }).ToList();

            var pdfList = pdfFiles.Select((p, index) => new
            {
                SN = index + 1,
                p.Id,
                p.Name,
                p.Category,
                p.DateOfUpload,
                p.Lock
            }).ToList();

            // Combine all files and get latest
            var allFiles = new List<dynamic>();
            allFiles.AddRange(audioFiles);
            allFiles.AddRange(videoFiles);
            allFiles.AddRange(pdfFiles);

            var latestFile = allFiles
                .OrderByDescending(f => ((dynamic)f).DateOfUpload)
                .FirstOrDefault();

            return Ok(new
            {
                AudioFiles = audioList,
                VideoFiles = videoList,
                PdfFiles = pdfList,
                LatestFile = latestFile
            });
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile(Guid fileId, string fileType)
        {
            object file = fileType.ToLower() switch
            {
                "audio" => await _context.AudioFiles.FindAsync(fileId),
                "video" => await _context.VideoFiles.FindAsync(fileId),
                "pdf" => await _context.PdfFiles.FindAsync(fileId),
                _ => null
            };

            if (file == null)
                return NotFound("File not found.");

            bool isLocked = fileType.ToLower() switch
            {
                "audio" => ((AudioFile)file).Lock,
                "video" => ((VideoFile)file).Lock,
                "pdf" => ((PdfFile)file).Lock,
                _ => true
            };

            if (isLocked)
                return Forbid("This file is locked.");

            byte[] fileBytes = fileType.ToLower() switch
            {
                "audio" => ((AudioFile)file).EncryptedContent,
                "video" => ((VideoFile)file).EncryptedContent,
                "pdf" => ((PdfFile)file).EncryptedContent,
                _ => null

            };

            if (fileBytes == null || fileBytes.Length == 0)
                return NotFound("File content not available.");

            string contentType = fileType.ToLower() switch
            {
                "audio" => "audio/mpeg",
                "video" => "video/mp4",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            string extension = fileType.ToLower() switch
            {
                "audio" => ".mp3",
                "video" => ".mp4",
                "pdf" => ".pdf",
                _ => ""
            };

            string originalName = ((dynamic)file).Name;
            string fileName = Path.GetFileNameWithoutExtension(originalName) + extension;

            return File(fileBytes, contentType, fileName);
        }

    }
}
