using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sit_in_Monitoring.Data;
using Sit_in_Monitoring.Models;

namespace Sit_in_Monitoring.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public AdminController(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    idNumber = r.User != null ? r.User.IDNumber : "—",
                    name = r.User != null ? r.User.FirstName + " " + r.User.LastName : "Unknown Student",
                    r.Lab,
                    pcNumber = r.PCNumber ?? "—",
                    date = r.Date.ToString("MMM dd, yyyy"),
                    time = r.Time.ToString(@"hh\:mm"),
                    r.Purpose,
                    r.Status
                })
                .ToListAsync();

            return Json(reservations);
        }

        [HttpGet]
        public async Task<IActionResult> GetLabPCs()
        {
            var pcs = await _context.LabPCs
                .OrderBy(p => p.Lab)
                .ThenBy(p => p.PCNumber)
                .ToListAsync();
            return Json(new { success = true, pcs });
        }

        [HttpPost]
        public async Task<IActionResult> AddLabPC(string lab, string pcNumber)
        {
            if (string.IsNullOrWhiteSpace(lab) || string.IsNullOrWhiteSpace(pcNumber))
                return Json(new { success = false, message = "Lab and PC Number are required." });

            var exists = await _context.LabPCs.AnyAsync(p => p.Lab == lab && p.PCNumber == pcNumber);
            if (exists)
                return Json(new { success = false, message = "This PC already exists in this lab." });

            var pc = new LabPC { Lab = lab, PCNumber = pcNumber, IsAvailable = true };
            _context.LabPCs.Add(pc);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "PC added successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> TogglePCAvailability(int id)
        {
            var pc = await _context.LabPCs.FindAsync(id);
            if (pc == null) return Json(new { success = false });

            pc.IsAvailable = !pc.IsAvailable;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLabPC(int id)
        {
            var pc = await _context.LabPCs.FindAsync(id);
            if (pc == null) return Json(new { success = false });

            _context.LabPCs.Remove(pc);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReservationStatus(int id, string status)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return Json(new { success = false });

            reservation.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(string Title, string Content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Content))
                    return Json(new { success = false, message = "Title and Content are required." });

                var announcement = new Announcement
                {
                    Title = Title,
                    Content = Content,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Announcement created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error creating announcement: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RegisterSitin(string IdNumber, string Purpose, string Lab)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(IdNumber) || string.IsNullOrWhiteSpace(Purpose) || string.IsNullOrWhiteSpace(Lab))
                    return Json(new { success = false, message = "All fields are required." });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.IDNumber == IdNumber);
                if (user == null)
                    return Json(new { success = false, message = "Student ID not found." });

                if (user.IsCurrentlyCheckedIn)
                    return Json(new { success = false, message = "Student is already checked in." });

                if (user.SessionsRemaining <= 0)
                    return Json(new { success = false, message = "Student has no sessions remaining." });

                var sitIn = new SitIn
                {
                    UserId = user.Id,
                    CheckInTime = DateTime.Now,
                    Notes = $"Lab: {Lab}, Purpose: {Purpose}"
                };
                _context.SitIns.Add(sitIn);

                user.IsCurrentlyCheckedIn = true;
                user.LastCheckIn = DateTime.Now;
                user.SessionsUsed++;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Successfully Registered!", sessionsRemaining = user.SessionsRemaining });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentSitins()
        {
            var sitins = await _context.SitIns
                .Include(s => s.User)
                .Where(s => s.CheckOutTime == null)
                .OrderByDescending(s => s.CheckInTime)
                .Select(s => new
                {
                    s.Id,
                    idNumber = s.User != null ? s.User.IDNumber : "—",
                    name = s.User != null ? s.User.FirstName + " " + s.User.LastName : "Unknown Student",
                    lab = s.Notes != null && s.Notes.Contains("Lab:") ? s.Notes.Split("Lab:")[1].Split(",")[0].Trim() : "—",
                    purpose = s.Notes != null && s.Notes.Contains("Purpose:") ? s.Notes.Split("Purpose:")[1].Trim() : "—",
                    timeIn = s.CheckInTime.ToString("MM/dd/yyyy hh:mm tt")
                })
                .ToListAsync();

            return Json(new { success = true, sitins });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSitins()
        {
            var sitins = await _context.SitIns
                .Include(s => s.User)
                .OrderByDescending(s => s.CheckInTime)
                .Select(s => new
                {
                    s.Id,
                    idNumber = s.User != null ? s.User.IDNumber : "—",
                    name = s.User != null ? s.User.FirstName + " " + s.User.LastName : "Unknown Student",
                    lab = s.Notes != null && s.Notes.Contains("Lab:") ? s.Notes.Split("Lab:")[1].Split(",")[0].Trim() : "—",
                    purpose = s.Notes != null && s.Notes.Contains("Purpose:") ? s.Notes.Split("Purpose:")[1].Trim() : "—",
                    timeIn = s.CheckInTime.ToString("MM/dd/yyyy hh:mm tt"),
                    timeOut = s.CheckOutTime.HasValue ? s.CheckOutTime.Value.ToString("MM/dd/yyyy hh:mm tt") : (string?)null,
                    duration = s.DurationInMinutes,
                    feedback = s.Feedback
                })
                .ToListAsync();

            return Json(new { success = true, sitins });
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            var announcements = await _context.Announcements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Content,
                    createdAt = a.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                    a.IsActive
                })
                .ToListAsync();

            return Json(new { success = true, announcements });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
                return Json(new { success = false, message = "Announcement not found." });

            announcement.IsActive = false;
            _context.Announcements.Update(announcement);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Announcement deleted." });
        }

        [HttpPost]
        public async Task<IActionResult> EndSitin(int id)
        {
            var sitIn = await _context.SitIns.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (sitIn == null)
                return Json(new { success = false, message = "Record not found." });

            sitIn.CheckOutTime = DateTime.Now;
            sitIn.DurationInMinutes = (int)Math.Round((sitIn.CheckOutTime.Value - sitIn.CheckInTime).TotalMinutes);

            if (sitIn.User != null)
            {
                sitIn.User.IsCurrentlyCheckedIn = false;
                sitIn.User.LastCheckOut = DateTime.Now;
                await _userManager.UpdateAsync(sitIn.User);
            }

            _context.SitIns.Update(sitIn);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sit-in ended.", sitinId = sitIn.Id, userId = sitIn.UserId });
        }

        

        [HttpPost]
        public async Task<IActionResult> AddReward(int sitinId)
        {
            var sitIn = await _context.SitIns.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sitinId);
            if (sitIn == null)
                return Json(new { success = false, message = "Record not found." });

            if (sitIn.User == null)
                return Json(new { success = false, message = "Student not found." });

            // Add 1 point
            sitIn.User.Points += 1;
            await _userManager.UpdateAsync(sitIn.User);

            // Send notification to student
            var notification = new StudentNotification
            {
                UserId = sitIn.User.Id,
                Title = "You received a reward point!",
                Message = $"The administrator awarded you 1 point for your sit-in session. Your total points: {sitIn.User.Points}.",
                CreatedAt = DateTime.Now
            };
            _context.StudentNotifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Reward added and student notified.", totalPoints = sitIn.User.Points });
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard()
        {
            try
            {
                var leaderboard = await _context.Users
                    .Where(u => u.SessionsUsed > 0) // Only include active participants
                    .OrderByDescending(u => u.SessionsUsed) // Rank by most sessions used
                    .ThenByDescending(u => u.Points)       // Tie-breaker: most reward points
                    .Take(10)                             // Limit to Top 10 students
                    .Select((u, index) => new
                    {
                        rank = 0, // We will calculate the index dynamically on client-side or below
                        idNumber = u.IDNumber,
                        name = u.FirstName + " " + u.LastName,
                        course = u.Course,
                        yearLevel = u.YearLevel,
                        sessions = u.SessionsUsed,
                        points = u.Points
                    })
                    .ToListAsync();

                // Project with sequential row indexing
                var rankedLeaderboard = leaderboard.Select((u, index) => new
                {
                    rank = index + 1,
                    u.idNumber,
                    u.name,
                    u.course,
                    u.yearLevel,
                    u.sessions,
                    u.points
                }).ToList();

                return Json(new { success = true, leaderboard = rankedLeaderboard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error loading leaderboard: {ex.Message}" });
            }
        }
    }
}
