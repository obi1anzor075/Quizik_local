using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Data.SqlClient;

namespace PresentationLayer.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly DataStoreDbContext _dbContext;
        private readonly CultureHelper _cultureHelper;

        public GameController(DataStoreDbContext dbContext, CultureHelper cultureHelper)
        {
            _dbContext = dbContext;
            _cultureHelper = cultureHelper;
        }

        private int CurrentQuestionIndex
        {
            get
            {
                int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int index);
                return index;
            }
            set
            {
                Response.Cookies.Append("CurrentQuestionIndex", value.ToString());
            }
        }

        private string GetCurrentLanguage()
        {
            string currentCulture = _cultureHelper.GetCurrentCulture();
            return currentCulture switch
            {
                "ru-RU" => string.Empty,
                "en-US" => "_us",
                "zh-CN" => "_cn",
                _ => string.Empty
            };
        }

        [HttpGet("/Game/CheckAnswer/{gameMode}/{selectedAnswer}")]
        public async Task<IActionResult> CheckAnswer(string gameMode, string selectedAnswer)
        {
            string category = gameMode.Replace("Easy", "").Replace("Duel", "");

            // Извлекаем текущий вопрос
            var question = _dbContext.Questions
                .Where(q => q.Category == category)
                .OrderBy(q => q.QuestionId)
                .Skip(CurrentQuestionIndex)
                .Take(1)
                .FirstOrDefault();

            if (question == null)
            {
                return Json(new { isCorrect = false, error = "Question not found." });
            }

            // Определяем правильный ответ по индексу
            string correctAnswerText = question.CorrectAnswerIndex switch
            {
                1 => question.Answer1,
                2 => question.Answer2,
                3 => question.Answer3,
                4 => question.Answer4,
                _ => string.Empty
            };

            // Сравнение выбранного ответа с правильным
            bool isCorrect = (selectedAnswer.Trim().Normalize() == correctAnswerText.Trim().Normalize());
            CurrentQuestionIndex++;

            if (isCorrect)
            {
                int.TryParse(Request.Cookies["CorrectAnswersCount"], out int correctAnswersCount);
                correctAnswersCount++;
                Response.Cookies.Append("CorrectAnswersCount", correctAnswersCount.ToString());
            }
            // Возвращаем результат проверки обратно на клиент
            return Json(new { isCorrect });
        }

        [HttpGet("/Game/CheckHardAnswer/{gameMode}/{selectedAnswer}")]
        public async Task<IActionResult> CheckHardAnswer(string gameMode, string selectedAnswer)
        {
            string category = gameMode.Replace("Hard", "");

            // Получаем текущий индекс вопроса из куки            
            int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int currentQuestionIndex);

            // Получаем текущий вопрос из базы данных
            var question = _dbContext.HardQuestions
                .Where(q => q.Category == category)
                .OrderBy(q => q.QuestionId)
                .Skip(currentQuestionIndex)
                .Take(1)
                .FirstOrDefault();

            // Получаем новый вопрос из базы данных
            var newQuestion = _dbContext.HardQuestions
                .Where(q => q.Category == category)
                .OrderBy(q => q.QuestionId)
                .Skip(currentQuestionIndex)
                .Take(1)
                .FirstOrDefault();


            // Проверяем, совпадает ли выбранный ответ с правильным ответом
            bool isCorrect = (selectedAnswer.ToUpper().Trim().Normalize() == question.CorrectAnswer.ToUpper().Trim().Normalize() ||
                              selectedAnswer.ToUpper().Trim().Normalize() == question.CorrectAnswer2.ToUpper().Trim().Normalize());

            // Увеличиваем индекс текущего вопроса и обновляем в куки
            currentQuestionIndex++;
            Response.Cookies.Append("CurrentQuestionIndex", currentQuestionIndex.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(1) });

            // Проверяем правильность ответа и увеличиваем счетчик, если ответ правильный
            if (isCorrect)
            {
                // Увеличиваем счетчик правильных ответов в куки
                int correctAnswersCount = 0;
                if (Request.Cookies["CorrectAnswersCount"] != null)
                {
                    int.TryParse(Request.Cookies["CorrectAnswersCount"], out correctAnswersCount);
                }
                correctAnswersCount++;
                Response.Cookies.Append("CorrectAnswersCount", correctAnswersCount.ToString(), new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddDays(1) 
                });
            }
            // Возвращаем результат проверки обратно на клиент
            return Json(new { isCorrect });
        }

        public IActionResult ResetCounters()
        {
            Response.Cookies.Delete("CorrectAnswersCount");
            Response.Cookies.Delete("CurrentQuestionIndex");
            return Ok();
        }


    }
}
