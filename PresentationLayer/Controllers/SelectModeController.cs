using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using DataAccessLayer.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;
using Microsoft.Data.SqlClient;
using PresentationLayer.Utilities;
using Microsoft.AspNetCore.Identity;
using BusinessLogicLayer.Services.Contracts;
using Newtonsoft.Json;

namespace PresentationLayer.Controllers
{
    [Authorize]
    public class SelectModeController : Controller
    {
        private readonly DataStoreDbContext _dbContext;
        private readonly IHubContext<GameHub, IChatClient> _gameHubContext;

        private readonly UserManager<User> _userManager;

        private readonly CultureHelper _cultureHelper;
        public readonly SharedViewLocalizer _localizer;

        private readonly IImageService _imageService;

        public SelectModeController(DataStoreDbContext dbContext, 
            IHubContext<GameHub, 
                IChatClient> gameHubContext, 
            UserManager<User> userManager,
            CultureHelper cultureHelper, 
            SharedViewLocalizer localizer, 
            IImageService imageService
        )
        {
            _dbContext = dbContext;
            _gameHubContext = gameHubContext;
            _userManager = userManager;
            _cultureHelper = cultureHelper;
            _localizer = localizer;
            _imageService = imageService;
        }

        private void ResetQuestionIndex()
        {
            Response.Cookies.Append("CurrentQuestionIndex", "0");
        }

        public IActionResult StartGame(string category, string difficulty)
        {

            // Выбираем 10 случайных вопросов для заданной категории.
            var questionIds = _dbContext.Questions
                .Where(q => q.Category == category)
                .OrderBy(q => Guid.NewGuid())  // Перемешиваем случайным образом
                .Take(10)
                .Select(q => q.QuestionId)
                .ToList();

            // Сохраняем список вопросов и текущий индекс в сессии
            HttpContext.Session.SetString("QuestionList", JsonConvert.SerializeObject(questionIds));
            HttpContext.Session.SetInt32("CurrentQuestionIndex", 0);
            // Сохраняем также данные о режиме игры и сложности, если они понадобятся в дальнейшем
            HttpContext.Session.SetString("GameMode", category);
            HttpContext.Session.SetString("Difficulty", difficulty);

            // Перенаправляем пользователя на нужный метод (Easy или Hard) с параметром category
            return RedirectToAction(difficulty == "Easy" ? "Easy" : "Hard", new { category });
        }


        public IActionResult EasyPDD()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Shared");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            string currentCulture = _cultureHelper.GetCurrentCulture();
            return StartGame("PDD", "Easy");
        }

        public IActionResult EasyGeography()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Shared");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            string currentCulture = _cultureHelper.GetCurrentCulture();
            return StartGame("Geography", "Easy");
        }

        public IActionResult EasyWWII()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Shared");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            string currentCulture = _cultureHelper.GetCurrentCulture();
            return StartGame("WWII", "Easy");
        }
        public IActionResult Easy(string category)
        {
            // Извлекаем список вопросов из сессии
            var questionListJson = HttpContext.Session.GetString("QuestionList");
            if (string.IsNullOrEmpty(questionListJson))
            {
                // Если список вопросов отсутствует, перенаправляем на старт игры
                return RedirectToAction("StartGame", new { gameMode = "Easy" + category });
            }

            var questionIds = JsonConvert.DeserializeObject<List<int>>(questionListJson);

            // Извлекаем текущий индекс вопроса из сессии
            int currentQuestionIndex = HttpContext.Session.GetInt32("CurrentQuestionIndex") ?? 0;

            // Если вопросов больше нет, завершаем игру
            if (currentQuestionIndex >= questionIds.Count)
            {
                string difficultyLevel = "Легкий";
                return RedirectToAction("Finish", new { difficultyLevel });
            }

            // Получаем ID текущего вопроса из списка
            int questionId = questionIds[currentQuestionIndex];
            var nextQuestion = _dbContext.Questions.FirstOrDefault(q => q.QuestionId == questionId);

            if (nextQuestion != null)
            {
                ViewBag.QuestionId = nextQuestion.QuestionId;
                ViewBag.QuestionText = nextQuestion.QuestionText;
                ViewBag.ImageUrl = _imageService.DecodeImageAsync(nextQuestion.ImageData, contentType: "image/jpeg");
                ViewBag.QuestionExplanation = nextQuestion.QuestionExplanation;

                var answers = new List<string>
                {
                    nextQuestion.Answer1,
                    nextQuestion.Answer2,
                    nextQuestion.Answer3,
                    nextQuestion.Answer4
                };

                // Перемешиваем варианты ответов
                var random = new Random();
                for (int i = answers.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = answers[i];
                    answers[i] = answers[j];
                    answers[j] = temp;
                }

                ViewBag.Answers = answers;

                // Увеличиваем индекс текущего вопроса в сессии для следующего запроса
                HttpContext.Session.SetInt32("CurrentQuestionIndex", currentQuestionIndex + 1);

                return View();
            }

            // Если вопрос не найден, завершаем игру
            string difficultyLevelFallback = "Легкий";
            return RedirectToAction("Finish", new { difficultyLevel = difficultyLevelFallback });
        }


        public IActionResult HardPDD()
        {
            return StartGame("PDD", "Hard");
        }

        public IActionResult HardGeography()
        {
            return StartGame("Geography", "Hard");
        }

        public IActionResult HardWWII()
        {
            return StartGame("WWII", "Hard");
        }

        public IActionResult Hard(string category)
        {
            // Извлекаем список вопросов из сессии
            var questionListJson = HttpContext.Session.GetString("QuestionList");
            if (string.IsNullOrEmpty(questionListJson))
            {
                // Если список вопросов отсутствует, перенаправляем на старт игры
                return RedirectToAction("StartGame", new { gameMode = "Hard" + category });
            }

            var questionIds = JsonConvert.DeserializeObject<List<int>>(questionListJson);

            // Извлекаем текущий индекс вопроса из сессии
            int currentQuestionIndex = HttpContext.Session.GetInt32("CurrentQuestionIndex") ?? 0;

            // Если вопросов больше нет, завершаем игру
            if (currentQuestionIndex >= questionIds.Count)
            {
                string difficultyLevel = "Сложный";
                return RedirectToAction("Finish", new { difficultyLevel });
            }

            // Получаем ID текущего вопроса из списка
            int questionId = questionIds[currentQuestionIndex];
            var nextQuestion = _dbContext.HardQuestions.FirstOrDefault(q => q.QuestionId == questionId);

            if (nextQuestion != null)
            {
                ViewBag.QuestionId = nextQuestion.QuestionId;
                ViewBag.QuestionText = nextQuestion.QuestionText;
                ViewBag.ImageUrl = _imageService.DecodeImageAsync(nextQuestion.ImageData);

                // Увеличиваем индекс текущего вопроса в сессии для следующего запроса
                HttpContext.Session.SetInt32("CurrentQuestionIndex", currentQuestionIndex + 1);

                return View();
            }

            // Если вопрос не найден, завершаем игру
            string difficultyLevelFallback = "Сложный";
            return RedirectToAction("Finish", new { difficultyLevel = difficultyLevelFallback });
        }


        public Task<IActionResult> DuelPDD()
        {
            return Duel("PDD");
        }

        public Task<IActionResult> DuelGeography()
        {
            return Duel("Geography");
        }
        
        public Task<IActionResult>DuelWWII()
        {
            return Duel("WWII");
        }

        public async Task<IActionResult> Duel(string category)
        {
            int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int currentQuestionIndex);

            var nextQuestion = _dbContext.Questions
                .Where(q => q.Category == category)
                .OrderBy(q => q.QuestionId)
                .Skip(currentQuestionIndex)
                .Take(1)
                .FirstOrDefault();

            if (nextQuestion != null)
            {
                ViewBag.QuestionId = nextQuestion.QuestionId;
                ViewBag.QuestionText = nextQuestion.QuestionText;
                ViewBag.ImageUrl = _imageService.DecodeImageAsync(nextQuestion.ImageData);
                ViewBag.QuestionExplanation = nextQuestion.QuestionExplanation;

                var answers = new List<string> { nextQuestion.Answer1, nextQuestion.Answer2, nextQuestion.Answer3, nextQuestion.Answer4 };

                var random = new Random();
                for (int i = answers.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = answers[i];
                    answers[i] = answers[j];
                    answers[j] = temp;
                }

                ViewBag.Answers = answers;

                await _gameHubContext.Clients.Group("DuelRoom").ReceiveQuestion(nextQuestion.QuestionId, 
                    nextQuestion.QuestionText, 
                    _imageService.DecodeImageAsync(nextQuestion.ImageData), 
                    nextQuestion.QuestionExplanation,answers
                );

                return View();
            }

            string difficultyLevel = "Дуэль";
            return RedirectToAction("Finish", new { difficultyLevel });
        }
        public async Task<IActionResult> Finish(string difficultyLevel)
        {
            // Получение пользователя
            var user = await _userManager.GetUserAsync(User);

            // Сохранение локализованного уровня сложности на английском
            string englishDifficultyLevel = difficultyLevel switch
            {
                "Легкий" => "Simple",  // Russian
                "Сложный" => "Hard",   // Russian
                "Дуэль" => "Duel",     // Russian
                "Simple" => "Simple",  // English
                "Hard" => "Hard",      // English
                "Duel" => "Duel",      // English
                "簡單的" => "Simple",    // Chinese
                "難的" => "Hard",      // Chinese
                "決鬥" => "Duel",      // Chinese
                _ => "Simple"         // Default
            };

            // Передача английского уровня сложности в ViewBag
            ViewBag.DifficultyLevel = englishDifficultyLevel;

            // Попытка получить количество правильных ответов из cookies
            if (!int.TryParse(Request.Cookies["CorrectAnswersCount"], out int correctAnswersCount))
            {
                correctAnswersCount = 0;
            }

            ViewBag.CorrectAnswersCount = correctAnswersCount;

            // Попытка получить общее количество вопросов из cookies
            if (!int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int totalQuestionsCount))
            {
                totalQuestionsCount = 0;
            }
            ViewBag.TotalQuestionsCount = totalQuestionsCount;

            // Сохранение результата в БД
            var result = new QuizResult
            {
                UserId = user.Id,
                Score = correctAnswersCount,
                Type = englishDifficultyLevel,
                DatePlayed = DateTime.Now
            };

            _dbContext.QuizResults.Add(result);
            await _dbContext.SaveChangesAsync();

            return View();
        }


        public IActionResult ResetCounters()
        {
            HttpContext.Session.SetInt32("CorrectAnswersCount", 0);
            HttpContext.Session.SetInt32("CurrentQuestionId", 0);

            return Ok();
        }

    }
}
