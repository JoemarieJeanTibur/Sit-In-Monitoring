using System.ComponentModel.DataAnnotations;

namespace Sit_in_Monitoring.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Student ID")]
        public required string StudentID { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
