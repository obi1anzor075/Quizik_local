using BusinessLogicLayer.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminTokenService _adminTokenService;

        public AdminController(IAdminTokenService adminTokenService)
        {
            _adminTokenService = adminTokenService;
        }

        public async Task<IActionResult> GenerateAdminLink()
        {
            var token = await _adminTokenService.GenerateTokenAsync();
            var adminLink = Url.Action("Register", "Home", new { token }, Request.Scheme);
            return Content($"Ссылка для регистрации администратора: {adminLink}");
        }

        public IActionResult AddQuiz()
        {
            return View();
        }
    }
}
