using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DRM.Data;
using DRM.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DRM.Configuration;
using Microsoft.Extensions.Hosting;
using DRM.Services; // Import the service namespace
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace DRM.Controllers
{
    [Authorize]
    public class ViewContentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ViewContentController> _logger;
        private readonly IFileEncryptionService _fileEncryptionService; // Inject the encryption service

        private readonly IWebHostEnvironment _environment;

        public ViewContentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ViewContentController> logger,
            IFileEncryptionService fileEncryptionService, // Add to constructor
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _fileEncryptionService = fileEncryptionService; // Store the service
            _environment = environment;
        }

        // View content listing
        public async Task<IActionResult> ContentVisualization()
        {
            var audioFiles = await _context.AudioFiles.OrderByDescending(a => a.DateOfUpload).ToListAsync();
            var videoFiles = await _context.VideoFiles.OrderByDescending(v => v.DateOfUpload).ToListAsync();
            var pdfFiles = await _context.PdfFiles.OrderByDescending(p => p.DateOfUpload).ToListAsync();

            ViewBag.AudioFiles = audioFiles.Select((a, index) => new { SN = index + 1, a.Id, a.Name, a.Category, a.DateOfUpload, a.Lock }).ToList();
            ViewBag.VideoFiles = videoFiles.Select((v, index) => new { SN = index + 1, v.Id, v.Name, v.Category, v.DateOfUpload, v.Lock }).ToList();
            ViewBag.PdfFiles = pdfFiles.Select((p, index) => new { SN = index + 1, p.Id, p.Name, p.Category, p.DateOfUpload, p.Lock }).ToList();

            var latestFile = new[] { audioFiles.Cast<object>(), videoFiles, pdfFiles }
                .SelectMany(f => f)
                .OrderByDescending(f => ((dynamic)f).DateOfUpload)
                .FirstOrDefault();

            ViewBag.LatestFile = latestFile;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestAccess(Guid fileId, string fileType)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Unauthorized();

                bool alreadyRequested = await _context.Requests.AnyAsync(r =>
                    r.UserId == userId &&
                    ((fileType == "audio" && r.AudioId == fileId) ||
                     (fileType == "video" && r.VideoId == fileId) ||
                     (fileType == "pdf" && r.PdfId == fileId)));

                if (alreadyRequested)
                    return BadRequest("You have already requested access to this file.");

                var request = new Requests
                {
                    UserId = userId,
                    RequestedDate = DateTime.UtcNow
                };

                switch (fileType)
                {
                    case "audio":
                        request.AudioId = fileId;
                        break;
                    case "video":
                        request.VideoId = fileId;
                        break;
                    case "pdf":
                        request.PdfId = fileId;
                        break;
                    default:
                        return BadRequest("Invalid file type.");
                }

                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                return View();
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(Guid fileId, string fileType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Check access
            bool hasAccess = await _context.AssignUsers.AnyAsync(a =>
                a.UserId == userId &&
                ((fileType == "audio" && a.AudioId == fileId) ||
                 (fileType == "video" && a.VideoId == fileId) ||
                 (fileType == "pdf" && a.PdfId == fileId)));

            if (!hasAccess)
            {
                _logger.LogWarning("Unauthorized download attempt. UserId: {UserId}, FileId: {FileId}, Type: {FileType}", userId, fileId, fileType);
                return Forbid("You do not have permission to download this file.");
            }

            // Get file from DB
            object file = fileType.ToLower() switch
            {
                "audio" => await _context.AudioFiles.FindAsync(fileId),
                "video" => await _context.VideoFiles.FindAsync(fileId),
                "pdf" => await _context.PdfFiles.FindAsync(fileId),
                _ => null
            };

            if (file == null)
            {
                _logger.LogWarning("File not found. FileId: {FileId}, Type: {FileType}", fileId, fileType);
                return NotFound();
            }

            byte[] fileBytes = fileType.ToLower() switch
            {
                "audio" => ((AudioFile)file).EncryptedContent,
                "video" => ((VideoFile)file).EncryptedContent,
                "pdf" => ((PdfFile)file).EncryptedContent,
                _ => null
            };

            if (fileBytes == null)
            {
                _logger.LogWarning("File content not found. FileId: {FileId}, Type: {FileType}", fileId, fileType);
                return NotFound();
            }

            // Ensure correct file extension
            string extension = fileType.ToLower() switch
            {
                "audio" => ".mp3",
                "video" => ".mp4",
                "pdf" => ".pdf",
                _ => ""
            };

            string originalName = ((dynamic)file).Name;
            string fileName = Path.GetFileName(originalName);
            string directoryPath = Path.Combine(_environment.WebRootPath, "Content", fileType.ToLower());
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string filePath = Path.Combine(directoryPath, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

            _logger.LogInformation("File saved to server for userId {UserId}: {Path}", userId, filePath);

            return Ok("File processed and saved to server.");
        }


        [HttpGet]
        public async Task<IActionResult> HasAccessToFile(Guid fileId, string fileType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool hasAccess = await _context.AssignUsers.AnyAsync(a =>
                a.UserId == userId &&
                ((fileType == "audio" && a.AudioId == fileId) ||
                 (fileType == "video" && a.VideoId == fileId) ||
                 (fileType == "pdf" && a.PdfId == fileId)));

            return Json(hasAccess);
        }



        public IActionResult DownloadView()
        {
            var pdfDirectory = Path.Combine(_environment.WebRootPath, "Content", "pdf");
            var audioDirectory = Path.Combine(_environment.WebRootPath, "Content", "audio");
            var videoDirectory = Path.Combine(_environment.WebRootPath, "Content", "video");

            // Get files from the directories
            var pdfFiles = Directory.GetFiles(pdfDirectory)
                .Select(f => new FileInfo(f))
                .Select(f => new
                {
                    Name = Path.GetFileNameWithoutExtension(f.Name), // cleaner for URL use
                    FullName = f.FullName,
                    Category = "PDF",
                    DateOfUpload = f.CreationTime,
                    type = "pdf",
                    Path = $"/Content/pdf/{f.Name}"
                })
                .OrderByDescending(f => f.DateOfUpload)
                .ToList();

            var audioFiles = Directory.GetFiles(audioDirectory)
                .Select(f => new FileInfo(f))
                .Select(f => new
                {
                    Name = Path.GetFileNameWithoutExtension(f.Name),
                    FullName = f.FullName,
                    Category = "Audio",
                    DateOfUpload = f.CreationTime,
                    type = "audio",
                    Path = $"/Content/audio/{f.Name}"
                })
                .OrderByDescending(f => f.DateOfUpload)
                .ToList();

            var videoFiles = Directory.GetFiles(videoDirectory)
                .Select(f => new FileInfo(f))
                .Select(f => new
                {
                    Name = Path.GetFileNameWithoutExtension(f.Name),
                    FullName = f.FullName,
                    Category = "Video",
                    DateOfUpload = f.CreationTime,
                    type = "video",
                    Path = $"/Content/video/{f.Name}"
                })
                .OrderByDescending(f => f.DateOfUpload)
                .ToList();

            ViewBag.PdfFiles = pdfFiles;
            ViewBag.AudioFiles = audioFiles;
            ViewBag.VideoFiles = videoFiles;

            // Get the latest file
            var latestFile = new[] { pdfFiles, audioFiles, videoFiles }
                .SelectMany(f => f)
                .OrderByDescending(f => f.DateOfUpload)
                .FirstOrDefault();

            ViewBag.LatestFile = latestFile;

            return View();
        }


        [HttpGet("/ViewContent/SecureView")]
        public IActionResult SecureView(string fileName, string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(fileType))
                return BadRequest("File name or type is missing.");

            var fileFolder = fileType.ToLower() switch
            {
                "audio" => "audio",
                "video" => "video",
                "pdf" => "pdf",
                _ => null
            };

            if (fileFolder == null)
                return BadRequest("Invalid file type.");

            var contentRoot = Path.Combine(_environment.WebRootPath, "Content", fileFolder);

            // Look for the actual file (case-insensitive, ignore extension)
            var matchedFile = Directory.GetFiles(contentRoot)
                .FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));

            if (matchedFile == null)
                return NotFound("File not found.");

            // Create the model using filename and type
            var model = new SecureViewModel
            {
                FileName = Path.GetFileName(matchedFile),
                FileType = fileType.ToLower()
            };

            return View(model);
        }


    }
}