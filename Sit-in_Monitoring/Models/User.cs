using Microsoft.AspNetCore.Identity;

namespace Sit_in_Monitoring.Models
{
    public class User : IdentityUser
    {
        public required string IDNumber { get; set; }
        public required string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public required string LastName { get; set; }
        public required string Course { get; set; }
        public required string YearLevel { get; set; }
        public required string Address { get; set; }
        public byte[]? ProfilePicture { get; set; }

        // Session tracking
        public int TotalSessionsAllowed { get; set; } = 30;
        public int SessionsUsed { get; set; } = 0;
        public DateTime? LastCheckIn { get; set; }
        public DateTime? LastCheckOut { get; set; }
        public bool IsCurrentlyCheckedIn { get; set; } = false;

        // Navigation property
        public ICollection<SitIn>? SitIns { get; set; }

        public int SessionsRemaining => TotalSessionsAllowed - SessionsUsed;
    }
}
