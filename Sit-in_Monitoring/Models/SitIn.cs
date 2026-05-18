namespace Sit_in_Monitoring.Models
{
    public class SitIn
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User? User { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int DurationInMinutes { get; set; }
        public string? Notes { get; set; }
        public string? Feedback { get; set; }
    }
}
