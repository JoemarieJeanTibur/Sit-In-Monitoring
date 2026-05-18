using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sit_in_Monitoring.Data;
using Sit_in_Monitoring.Models;

namespace Sit_in_Monitoring.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public StudentController(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (user.IsCurrentlyCheckedIn)
                return Json(new { success = false, message = "You are already checked in!" });

            if (user.SessionsRemaining <= 0)
                return Json(new { success = false, message = "No sessions remaining!" });

            var sitIn = new SitIn
            {
                UserId = user.Id,
                CheckInTime = DateTime.Now
            };

            _context.SitIns.Add(sitIn);
            user.LastCheckIn = DateTime.Now;
            user.IsCurrentlyCheckedIn = true;
            user.SessionsUsed++;

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, message = "Checked in successfully!", sessionsRemaining = user.SessionsRemaining });
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (!user.IsCurrentlyCheckedIn)
                return Json(new { success = false, message = "You are not currently checked in!" });

            var currentSitIn = await _context.SitIns
                .Where(s => s.UserId == user.Id && s.CheckOutTime == null)
                .FirstOrDefaultAsync();

            if (currentSitIn == null)
                return Json(new { success = false, message = "No active check-in found." });

            currentSitIn.CheckOutTime = DateTime.Now;
            currentSitIn.DurationInMinutes = (int)Math.Round((currentSitIn.CheckOutTime.Value - currentSitIn.CheckInTime).TotalMinutes);

            user.LastCheckOut = DateTime.Now;
            user.IsCurrentlyCheckedIn = false;

            _context.SitIns.Update(currentSitIn);
            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, message = "Checked out successfully!", sessionsRemaining = user.SessionsRemaining });
        }

        [HttpGet]
        public async Task<IActionResult> GetSessionStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                isCheckedIn = user.IsCurrentlyCheckedIn,
                sessionsRemaining = user.SessionsRemaining,
                sessionsUsed = user.SessionsUsed,
                lastCheckIn = user.LastCheckIn?.ToString("g"),
                lastCheckOut = user.LastCheckOut?.ToString("g")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                var fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();

                // Validate file size
                if (profilePicture.Length > maxFileSize)
                {
                    return Json(new { success = false, message = "File size must be less than 5 MB." });
                }

                // Validate file type
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Only image files (jpg, jpeg, png, gif) are allowed." });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                using (var memoryStream = new MemoryStream())
                {
                    await profilePicture.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Profile picture updated successfully!" });
                }

                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = $"Error updating profile picture: {errorMessage}" });
            }

            return Json(new { success = false, message = "No file was uploaded." });
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetProfilePicture(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.ProfilePicture != null && user.ProfilePicture.Length > 0)
            {
                return File(user.ProfilePicture, "image/jpeg");
            }

            // Return a default placeholder image
            return Content("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='100'%3E%3Crect fill='%23ddd' width='100' height='100'/%3E%3C/svg%3E", "image/svg+xml");
        }

        public async Task<IActionResult> Reservation()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(string lab, DateTime date, TimeSpan time, string purpose, string pcNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var reservation = new Reservation
            {
                UserId = user.Id,
                Lab = lab,
                Date = date,
                Time = time,
                Purpose = purpose,
                PCNumber = pcNumber,
                Status = "Pending"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Reservation submitted successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailablePCs(string lab)
        {
            var pcs = await _context.LabPCs
                .Where(p => p.Lab == lab && p.IsAvailable)
                .OrderBy(p => p.PCNumber)
                .Select(p => p.PCNumber)
                .ToListAsync();
            return Json(new { success = true, pcs });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string address, string course, string yearLevel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found" });

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Address = address;
            user.Course = course;
            user.YearLevel = yearLevel;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Profile updated successfully!" });
            }

            return Json(new { success = false, message = "Failed to update profile." });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReservations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false });

            var reservations = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Lab,
                    r.PCNumber,
                    date = r.Date.ToString("MMM dd, yyyy"),
                    time = r.Time.ToString(@"hh\:mm"),
                    r.Purpose,
                    r.Status
                })
                .ToListAsync();
            return Json(new { success = true, reservations });
        }

        [HttpGet]
        public async Task<IActionResult> GetSitinHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var history = await _context.SitIns
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CheckInTime)
                .Select(s => new
                {
                    s.Id,
                    checkInTime = s.CheckInTime.ToString("MMM dd, yyyy hh:mm tt"),
                    checkOutTime = s.CheckOutTime.HasValue ? s.CheckOutTime.Value.ToString("MMM dd, yyyy hh:mm tt") : null,
                    duration = s.DurationInMinutes,
                    notes = s.Notes,
                    feedback = s.Feedback,
                    isActive = s.CheckOutTime == null
                })
                .ToListAsync();

            return Json(new { success = true, history });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int sitinId, string feedback)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found" });

            var sitIn = await _context.SitIns.FindAsync(sitinId);
            if (sitIn == null || sitIn.UserId != user.Id)
                return Json(new { success = false, message = "Sit-in record not found" });

            if (sitIn.CheckOutTime == null)
                return Json(new { success = false, message = "Session must be ended before providing feedback" });

            sitIn.Feedback = feedback;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Feedback submitted successfully!" });
        }

        public IActionResult History() => View();
        public IActionResult Notifications() => View();
        public IActionResult Rewards() => View();

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
                    createdAt = a.CreatedAt.ToString("MMM dd, yyyy hh:mm tt")
                })
                .ToListAsync();

            return Json(new { success = true, announcements });
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestAnnouncements(int count = 5)
        {
            var announcements = await _context.Announcements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Content,
                    createdAt = a.CreatedAt.ToString("MMM dd, yyyy hh:mm tt")
                })
                .ToListAsync();

            return Json(new { success = true, announcements });
        }

        public async Task<IActionResult> Leaderboard()
        {
            var users = await _context.Users
                .Where(u => u.SessionsUsed > 0)
                .OrderByDescending(u => u.SessionsUsed)
                .ThenByDescending(u => u.Points)
                .Take(20)
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.IDNumber,
                    u.Course,
                    u.YearLevel,
                    u.SessionsUsed,
                    u.Points
                })
                .ToListAsync();

            return View(users.Select((u, i) => new
            {
                Rank = i + 1,
                u.FirstName,
                u.LastName,
                u.IDNumber,
                u.Course,
                u.YearLevel,
                u.SessionsUsed,
                u.Points
            }).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard()
        {
            try
            {
                var leaderboard = await _context.Users
                    .Where(u => u.SessionsUsed > 0)
                    .OrderByDescending(u => u.SessionsUsed)
                    .ThenByDescending(u => u.Points)
                    .Take(10)
                    .Select(u => new
                    {
                        u.FirstName,
                        u.LastName,
                        u.IDNumber,
                        u.Course,
                        u.YearLevel,
                        u.SessionsUsed,
                        u.Points
                    })
                    .ToListAsync();

                var rankedLeaderboard = leaderboard.Select((u, index) => new
                {
                    Rank = index + 1,
                    u.FirstName,
                    u.LastName,
                    u.IDNumber,
                    u.Course,
                    u.YearLevel,
                    u.SessionsUsed,
                    u.Points
                }).ToList();

                return Json(new { success = true, leaderboard = rankedLeaderboard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error loading leaderboard: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false });

            var notifications = await _context.StudentNotifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    createdAt = n.CreatedAt.ToString("MMM dd, yyyy hh:mm tt")
                })
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.IsRead);

            return Json(new { success = true, notifications, unreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var unread = await _context.StudentNotifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
