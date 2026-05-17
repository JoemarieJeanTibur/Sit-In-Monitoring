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
            var user = await _userManager.GetUserAsync(User);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    idNumber = r.User.IDNumber,
                    name = r.User.FirstName + " " + r.User.LastName,
                    r.Lab,
                    date = r.Date.ToString("MMM dd, yyyy"),
                    time = r.Time.ToString(@"hh\:mm"),
                    r.Purpose,
                    r.Status
                })
                .ToListAsync();

            return Json(reservations);
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
        public IActionResult CreateAnnouncement(string Title, string Content)
        {
            // Placeholder for now
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterSitin(string IdNumber, string Name, string Purpose, string Lab)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.IDNumber == IdNumber);
            if (user != null)
            {
                var sitIn = new SitIn
                {
                    UserId = user.Id,
                    CheckInTime = DateTime.Now,
                    Notes = $"Lab: {Lab}, Purpose: {Purpose}"
                };
                _context.SitIns.Add(sitIn);
                
                user.IsCurrentlyCheckedIn = true;
                user.LastCheckIn = DateTime.Now;
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }
    }
}
