using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Contracts;
using BusinessLogicLayer.DTO;
using DataAccessLayer.Models;
using PresentationLayer.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminTokenService _adminTokenService;

        private readonly IQuizService _quizService;
        private readonly IImageService _imageService;

        public AdminController(IAdminTokenService adminTokenService, IQuizService quizService, IImageService imageService)
        {
            _adminTokenService = adminTokenService;
            _quizService = quizService;
            _imageService = imageService;
        }

        public async Task<IActionResult> GenerateAdminLink()
        {
            var token = await _adminTokenService.GenerateTokenAsync();
            var adminLink = Url.Action("Register", "Home", new { token }, Request.Scheme);
            return Content($"Ссылка для регистрации администратора: {adminLink}");
        }

        public async Task<IActionResult> AddQuiz()
        {
            IEnumerable<string> tablesNames = ["Легкий вопрос", "Сложный вопрос"];
            return View(tablesNames);
        }

        [HttpPost]
        public async Task<IActionResult> AddQuestion([FromForm] QuestionVM model)
        {
            byte[] imageData = await _imageService.ProcessImageAsync(model.ImageFile);

            try
            {
                // Маппинг из ViewModel в Question
                var question = new Question
                {
                    QuestionText = model.QuestionText,
                    CorrectAnswerIndex = model.CorrectAnswerIndex,
                    Answer1 = model.Answer1,
                    Answer2 = model.Answer2,
                    Answer3 = model.Answer3,
                    Answer4 = model.Answer4,
                    Category = model.Category,
                    QuestionExplanation = model.QuestionExplanation,
                    ImageData = imageData
                };
                await _quizService.AddQuestionAsync(question);
                return Json(new { success = true, message = "Вопрос успешно добавлен!" });
            }
            catch (ArgumentException argEx)
            {
                // Если возникла ошибка валидации имени таблицы или другая аргументная ошибка
                return Json(new { success = false, message = "Ошибка: " + argEx.Message });
            }
            catch (Exception ex)
            {
                // Общая обработка ошибок
                return Json(new { success = false, message = "Ошибка при добавлении вопроса: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddHardQuestion([FromForm] HardQuestionVM model)
        {
            byte[] imageData = await _imageService.ProcessImageAsync(model.ImageFile);

            try
            {
                var hardQuestion = new HardQuestion
                {
                    QuestionText = model.QuestionText,
                    CorrectAnswer = model.CorrectAnswer,
                    CorrectAnswer2 = model.CorrectAnswer2,
                    QuestionExplanation = model.QuestionExplanation,
                    Category = model.Category,
                    ImageData = imageData,
                };
                await _quizService.AddHardQuestionAsync(hardQuestion);
                return Json(new { success = true, message = "Вопрос успешно добавлен!" });
            }
            catch (ArgumentException argEx)
            {
                // Если возникла ошибка валидации имени таблицы или другая аргументная ошибка
                return Json(new { success = false, message = "Ошибка: " + argEx.Message });
            }
            catch (Exception ex)
            {
                // Общая обработка ошибок
                return Json(new { success = false, message = "Ошибка при добавлении вопроса: " + ex.Message });
            }
        }
    }
}
