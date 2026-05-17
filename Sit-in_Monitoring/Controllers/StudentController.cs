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

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, message = "Checked in successfully!" });
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
            user.SessionsUsed++;

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
    }
}
