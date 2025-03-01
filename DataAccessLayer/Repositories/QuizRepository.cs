using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly DataStoreDbContext _context;

        public QuizRepository(DataStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QuizResult>> GetQuizResultsByUserIdAsync(string userId)
        {
            return await _context.QuizResults
                                 .Where(qr => qr.UserId == userId)
                                 .OrderByDescending(qr => qr.DatePlayed)
                                 .ToListAsync();
        }


        public async Task<IEnumerable<Question>> GetAllQuestionsAsync()
        {
            return await _context.Questions
                .OrderBy(q => q.QuestionId)
                .ToListAsync();
        }   
        
        public async Task<IEnumerable<HardQuestion>> GetAllHardQuestionsAsync()
        {
            return await _context.HardQuestions
                .OrderBy(q => q.QuestionId)
                .ToListAsync();
        }

        public async Task AddQuestionAsync(Question question, string tableName)
        {
            // Допустимые префиксы для таблиц
            string[] allowedPrefixes = new string[] { "Easy", "Duel" };

            // Проверка, начинается ли таблица с этих прейфиксов
            bool isAllowed = allowedPrefixes.Any(prefix => tableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (isAllowed)
            {
                throw new ArgumentException("Недопустимое имя таблицы", nameof(tableName));
            }

            // SQL запрос
            string sql = $"INSERT INTO {tableName} (QuestionText, ImageData, CorrectAnswer, Answer1, Answer2, Answer3, Answer4, question_explanation) " +
                 "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)";

            await _context.Database.ExecuteSqlRawAsync(
                sql,
                question.QuestionText,
                question.ImageData,
                question.CorrectAnswer,
                question.Answer1,
                question.Answer2,
                question.Answer3,
                question.Answer4,
                question.QuestionExplanation
            );
        }

        public async Task AddHardQuestionAsync(HardQuestion hardQuestion, string tableName)
        {
            // Допустимые префиксы для таблиц
            string allowedPrefix =  "Hard";

            // Проверка, начинается ли таблица с этих прейфиксов
            if (!tableName.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Имя таблицы для сложных вопросов должно начинаться с 'Hard'", nameof(tableName));
            }

            // SQL запрос
            string sql = $"INSERT INTO {tableName} (QuestionText, ImageData, CorrectAnswer, CorrectAnswer2) " +
                 "VALUES (@p0, @p1, @p2, @p3)";

            await _context.Database.ExecuteSqlRawAsync(
                sql,
                hardQuestion.QuestionText,
                hardQuestion.ImageData,
                hardQuestion.CorrectAnswer,
                hardQuestion.CorrectAnswer2
            );
        }

    }
}
