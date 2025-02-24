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

        public SelectModeController(DataStoreDbContext dbContext, IHubContext<GameHub, IChatClient> gameHubContext, UserManager<User> userManager,CultureHelper cultureHelper, SharedViewLocalizer localizer)
        {
            _dbContext = dbContext;
            _gameHubContext = gameHubContext;
            _userManager = userManager;
            _cultureHelper = cultureHelper;
            _localizer = localizer;
        }

        private void ResetQuestionIndex()
        {
            Response.Cookies.Append("CurrentQuestionIndex", "0");
        }

        public IActionResult EasyPDD()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Shared");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            string currentCulture = _cultureHelper.GetCurrentCulture();
            return currentCulture switch
            {
                "ru-RU" => Easy("EasyPDD"),
                "en-US" => Easy("EasyPDD_us"),
                "zh-CN" => Easy("EasyPDD_cn"),
                _ => Easy("EasyPDD")
            };
        }

        public IActionResult EasyGeography()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Shared");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            string currentCulture = _cultureHelper.GetCurrentCulture();
            return currentCulture switch
            {
                "ru-RU" => Easy("EasyGeography"),
                "en-US" => Easy("EasyGeography_us"),
                "zh-CN" => Easy("EasyGeography_cn"),
                _ => Easy("EasyGeography")
            };
        }

        public IActionResult EasyWWII()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Shared");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            string currentCulture = _cultureHelper.GetCurrentCulture();
            return currentCulture switch
            {
                "ru-RU" => Easy("EasyWWII"),
                "en-US" => Easy("EasyWWII_us"),
                "zh-CN" => Easy("EasyWWII_cn"),
                _ => Easy("EasyWWII")
            };
        }

        public IActionResult Easy(string gameMode)
        {

            // Извлекаем текущее значение индекса вопроса из сессии
            int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int currentQuestionIndex);


            // Динамическое формирование SQL-запроса
            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {currentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";

            var nextQuestion = _dbContext.Questions.FromSqlRaw(sqlQuery).FirstOrDefault();

            if (nextQuestion != null)
            {
                ViewBag.QuestionId = nextQuestion.QuestionId;
                ViewBag.QuestionText = nextQuestion.QuestionText;
                ViewBag.ImageUrl = nextQuestion.ImageUrl;
                ViewBag.QuestionExplanation = nextQuestion.QuestionExplanation;

                var answers = new List<string> { nextQuestion.Answer1, nextQuestion.Answer2, nextQuestion.Answer3, nextQuestion.Answer4 };

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
                return View();
            }

            string difficultyLevel = "Легкий";
            return RedirectToAction("Finish", new { difficultyLevel });
        }

        public IActionResult HardPDD()
        {
            return Hard("HardPDD");
        }

        public IActionResult HardGeography()
        {
            return Hard("HardGeography");
        }

        public IActionResult HardWWII()
        {
            return Hard("HardWWII");
        }

        public IActionResult Hard(string gameMode)
        {

            int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int currentQuestionIndex);

            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {currentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";

            var nextQuestion = _dbContext.HardQuestions.FromSqlRaw(sqlQuery).FirstOrDefault();

            if (nextQuestion != null)
            {
                ViewBag.QuestionId = nextQuestion.QuestionId;
                ViewBag.QuestionText = nextQuestion.QuestionText;
                ViewBag.ImageUrl = nextQuestion.ImageUrl;

                return View();
            }

            string difficultyLevel = "Сложный";
            return RedirectToAction("Finish", new { difficultyLevel });
        }

        public Task<IActionResult> DuelPDD()
        {
            return Duel("DuelPDD");
        }

        public Task<IActionResult> DuelGeography()
        {
            return Duel("DuelGeography");
        }
        
        public Task<IActionResult>DuelWWII()
        {
            return Duel("DuelWWII");
        }

        public async Task<IActionResult> Duel(string gameMode)
        {
            int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int currentQuestionIndex);

            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {currentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";

            var nextQuestion = _dbContext.Questions.FromSqlRaw(sqlQuery).FirstOrDefault();

            if (nextQuestion != null)
            {
                ViewBag.QuestionId = nextQuestion.QuestionId;
                ViewBag.QuestionText = nextQuestion.QuestionText;
                ViewBag.ImageUrl = nextQuestion.ImageUrl;
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

                await _gameHubContext.Clients.Group("DuelRoom").ReceiveQuestion(nextQuestion.QuestionId, nextQuestion.QuestionText, nextQuestion.ImageUrl, nextQuestion.QuestionExplanation,answers);

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
