using DRM.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DRM.Models
{
    public class Requests
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; } // References IdentityUser ID
        public ApplicationUser User { get; set; } // Navigation Property

        [ForeignKey("VideoFile")]
        public Guid? VideoId { get; set; }
        public VideoFile VideoFile { get; set; } // Navigation Property

        [ForeignKey("PdfFile")]
        public Guid? PdfId { get; set; }
        public PdfFile PdfFile { get; set; } // Navigation Property

        [ForeignKey("AudioFile")]
        public Guid? AudioId { get; set; }
        public AudioFile AudioFile { get; set; } // Navigation Property

        [Required]
        public bool IsAccepted { get; set; } = false; // False by default until approved

        [Required]
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    }
}
