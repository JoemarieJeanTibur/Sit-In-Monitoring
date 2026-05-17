namespace Sit_in_Monitoring.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User? User { get; set; }
        public string Lab { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}