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

        public GameController(DataStoreDbContext dbContext)
        {
            _dbContext = dbContext;
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

        [HttpGet("/Game/CheckAnswer/{gameMode}/{selectedAnswer}")]
        public async Task<IActionResult> CheckAnswer(string gameMode, string selectedAnswer)
        {
            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {CurrentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";
            var question = _dbContext.Questions.FromSqlRaw(sqlQuery).FirstOrDefault();

            if (question == null)
            {
                return Json(new { isCorrect = false, error = "Question not found." });
            }

            bool isCorrect = (selectedAnswer.Trim().Normalize() == question.CorrectAnswer.Trim().Normalize());
            CurrentQuestionIndex++;

            if (isCorrect)
            {
                int.TryParse(Request.Cookies["CorrectAnswersCount"], out int correctAnswersCount);
                correctAnswersCount++;
                Response.Cookies.Append("CorrectAnswersCount", correctAnswersCount.ToString());
            }

            var sqlQueryCount = $"SELECT * FROM {gameMode}";
            int questionCount = await _dbContext.Questions.FromSqlRaw(sqlQueryCount).CountAsync();

            return Json(new { isCorrect });
        }



        [HttpGet("/Game/CheckHardAnswer/{gameMode}/{selectedAnswer}")]
        public async Task<IActionResult> CheckHardAnswer(string gameMode, string selectedAnswer)
        {
            // Получаем текущий индекс вопроса из куки            
            int.TryParse(Request.Cookies["CurrentQuestionIndex"], out int currentQuestionIndex);

            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {currentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";

            // Получаем текущий вопрос из базы данных
            var question = _dbContext.HardQuestions.FromSqlRaw(sqlQuery).FirstOrDefault();

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
                Response.Cookies.Append("CorrectAnswersCount", correctAnswersCount.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(1) });
            }

            var sqlQueryCount = $"SELECT * FROM {gameMode}";

            int questionCount = await _dbContext.HardQuestions
                .FromSqlRaw(sqlQueryCount)
                .CountAsync();

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
