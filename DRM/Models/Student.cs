using System;
using System.ComponentModel.DataAnnotations;

namespace DRM.Models
{
    public class Student
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string ?Email { get; set; }

        [StringLength(50)]
        public string ?EnrollmentNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set
            {
                if (value.HasValue)
                {
                    var dob = value.Value;
                    _dateOfBirth = dob.Kind == DateTimeKind.Utc
                        ? dob
                        : DateTime.SpecifyKind(dob, DateTimeKind.Utc);
                }
                else
                {
                    _dateOfBirth = null;
                }
            }
        }

        private DateTime? _dateOfBirth;

        [Required(ErrorMessage = "Grade is required.")]
        [StringLength(100)]
        public string Grade { get; set; }

        public string? Token { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
