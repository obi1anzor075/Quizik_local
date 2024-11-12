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

namespace PresentationLayer.Controllers
{
    [Authorize]
    public class SelectModeController : Controller
    {
        private readonly DataStoreDbContext _dbContext;
        private readonly IHubContext<GameHub, IChatClient> _gameHubContext;

        public SelectModeController(DataStoreDbContext dbContext, IHubContext<GameHub, IChatClient> gameHubContext)
        {
            _dbContext = dbContext;
            _gameHubContext = gameHubContext;
        }

        private void ResetQuestionIndex()
        {
            Response.Cookies.Append("CurrentQuestionIndex", "0");
        }

        public IActionResult EasyPDD()
        {
            
            return Easy("EasyPDD");
        }

        public IActionResult EasyGeography()
        {
            return Easy("EasyGeography");
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

                await _gameHubContext.Clients.Group("DuelRoom").ReceiveQuestion(nextQuestion.QuestionId, nextQuestion.QuestionText, nextQuestion.ImageUrl, answers);

                return View();
            }

            string difficultyLevel = "Дуэль";
            return RedirectToAction("Finish", new { difficultyLevel });
        }
        public IActionResult Finish(string difficultyLevel)
        {
            ViewBag.DifficultyLevel = difficultyLevel;

            // Attempt to retrieve the correct answers count from cookies, defaulting to 0 if it doesn't exist
            if (!int.TryParse(Request.Cookies["CorrectAnswersCount"], out int correctAnswersCount))
            {
                correctAnswersCount = 0;
            }

            ViewBag.CorrectAnswersCount = correctAnswersCount;

            if (!int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int totalQuestionsCount))
            {
                totalQuestionsCount = 0;
            }
            ViewBag.TotalQuestionsCount = totalQuestionsCount - 1;

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
