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
                return HttpContext.Session.GetInt32("CurrentQuestionIndex") ?? 0;
            }
            set
            {
                HttpContext.Session.SetInt32("CurrentQuestionIndex", value);
            }
        }


        [HttpGet("/Game/CheckAnswer/{gameMode}/{selectedAnswer}")]
        public async Task<IActionResult> CheckAnswer(string gameMode, string selectedAnswer)
        {
            // SQL запрос для получения вопроса
            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {CurrentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";

            // Получаем текущий вопрос из базы данных
            var question = _dbContext.Questions.FromSqlRaw(sqlQuery).FirstOrDefault();

            // Проверяем, был ли вопрос найден
            if (question == null)
            {
                // Логирование ошибки
                Console.WriteLine("Question not found for index: " + CurrentQuestionIndex);
                return Json(new { isCorrect = false, error = "Question not found." });
            }

            // Проверяем, совпадает ли выбранный ответ с правильным ответом
            bool isCorrect = (selectedAnswer == question.CorrectAnswer);
            CurrentQuestionIndex++;

            // Проверяем правильность ответа и увеличиваем счетчик, если ответ правильный
            if (isCorrect)
            {
                // Увеличиваем счетчик правильных ответов в сессии
                int correctAnswersCount = HttpContext.Session.GetInt32("CorrectAnswersCount") ?? 0;
                correctAnswersCount++;
                HttpContext.Session.SetInt32("CorrectAnswersCount", correctAnswersCount);
            }

            var sqlQueryCount = $"SELECT * FROM {gameMode}";

            int questionCount = await _dbContext.Questions.FromSqlRaw(sqlQueryCount).CountAsync();

            // Если ответили на все вопросы, сбрасываем счетчик
            if (CurrentQuestionIndex >= _dbContext.Questions.Count())
            {
                CurrentQuestionIndex = 0;
                HttpContext.Session.SetInt32("CurrentQuestionIndex", CurrentQuestionIndex);
            }

            HttpContext.Session.SetInt32("CurrentQuestionId", question.QuestionId);

            Console.WriteLine($"CurrentQuestionIndex: {CurrentQuestionIndex}");
            Console.WriteLine($"SQL Query: {sqlQuery}");

            // Возвращаем результат проверки и следующий вопрос обратно на клиент
            return Json(new { isCorrect = isCorrect });
        }

        [HttpGet("/Game/CheckHardAnswer/{gameMode}/{selectedAnswer}")]
        public async Task<IActionResult> CheckHardAnswer(string gameMode, string selectedAnswer)
        {
            // Получаем текущий индекс вопроса из сессии
            int currentQuestionIndex = HttpContext.Session.GetInt32("CurrentQuestionIndex") ?? 0;

            string sqlQuery = $"SELECT * FROM {gameMode} ORDER BY question_id OFFSET {CurrentQuestionIndex} ROWS FETCH NEXT 1 ROWS ONLY";

            // Получаем текущий вопрос из базы данных
            var question = _dbContext.HardQuestions.FromSqlRaw(sqlQuery).FirstOrDefault();

            // Проверяем, совпадает ли выбранный ответ с правильным ответом
            bool isCorrect = (selectedAnswer.ToUpper().Trim().Normalize() == question.CorrectAnswer.ToUpper().Trim().Normalize() || selectedAnswer.ToUpper().Trim().Normalize() == question.CorrectAnswer2.ToUpper().Trim().Normalize());

            // Увеличиваем индекс текущего вопроса и обновляем в сессии
            currentQuestionIndex++;
            HttpContext.Session.SetInt32("CurrentQuestionIndex", currentQuestionIndex);

            // Проверяем правильность ответа и увеличиваем счетчик, если ответ правильный
            if (isCorrect)
            {
                // Увеличиваем счетчик правильных ответов в сессии
                int correctAnswersCount = HttpContext.Session.GetInt32("CorrectAnswersCount") ?? 0;
                correctAnswersCount++;
                HttpContext.Session.SetInt32("CorrectAnswersCount", correctAnswersCount);
            }

            var sqlQueryCount = $"SELECT * FROM {gameMode}";

            int questionCount = await _dbContext.HardQuestions
                .FromSqlRaw(sqlQueryCount)
                .CountAsync();
            // Если ответили на все вопросы, сбрасываем счетчики
            if (currentQuestionIndex >= questionCount);
            {
                currentQuestionIndex = 0;
                HttpContext.Session.SetInt32("CurrentQuestionIndex", currentQuestionIndex);
            }

            HttpContext.Session.SetInt32("CurrentQuestionId", question.QuestionId);

            // Возвращаем результат проверки обратно на клиент
            return Json(new { isCorrect });
        }



        public IActionResult ResetCounters()
        {
            // Сброс счетчика верных ответов и CurrentQuestionId
            HttpContext.Session.Remove("CorrectAnswersCount");
            HttpContext.Session.Remove("CurrentQuestionId");
            HttpContext.Session.Remove("CurrentQuestionIndex");

            return Ok();
        }


    }
}
