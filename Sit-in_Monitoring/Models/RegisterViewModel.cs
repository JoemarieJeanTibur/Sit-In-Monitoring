using System.ComponentModel.DataAnnotations;

namespace Sit_in_Monitoring.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "ID Number")]
        public required string IDNumber { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public required string FirstName { get; set; }

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public required string LastName { get; set; }

        [Required]
        [Display(Name = "Course")]
        public required string Course { get; set; }

        [Required]
        [Display(Name = "Year Level")]
        public required string YearLevel { get; set; }

        [Required]
        [Display(Name = "Address")]
        public required string Address { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }
    }
}
