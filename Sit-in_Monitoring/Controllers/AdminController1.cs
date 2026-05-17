using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sit_in_Monitoring.Models;

namespace Sit_in_Monitoring.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var students = await Task.FromResult(
                _userManager.Users.Where(u => u.IDNumber != "1000").ToList()
            );
            ViewBag.TotalStudents = students.Count;
            return View();
        }

        public IActionResult Search() => View();
        public IActionResult StudentList() => View();
        public IActionResult SitIn() => View();
        public IActionResult SitInReports() => View();
        public IActionResult FeedbackReports() => View();
        public IActionResult Reservation() => View();
        public IActionResult GenerateReports() => View();
    }
}