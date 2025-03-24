using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DRM.Data; // Ensure this namespace includes ApplicationUser

public class AssignUser
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

    // ✅ Stores the date selected by the user
    [Required]
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    // ✅ Extracts the year from AssignedDate automatically
    [Required]
    public int Year { get; set; }

    // ✅ Before saving, automatically set the Year from AssignedDate
    public AssignUser()
    {
        Year = AssignedDate.Year;
    }
}
