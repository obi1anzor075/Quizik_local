using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
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

        public async Task AddQuestionAsync(Question question)
        {
            await _context.AddAsync(question);
        }


        public async Task AddHardQuestionAsync(HardQuestion question)
        {
            await _context.HardQuestions.AddAsync(question);
        }


        /// <summary>
        /// Получает список имён таблиц, начинающихся с "Easy", "Duel" или "Hard"
        /// </summary>
        /// <returns>Список имён таблиц</returns>
        public async Task<IEnumerable<string>> GetTableNamesAsync()
        {
            string sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'Easy%' OR TABLE_NAME LIKE 'Duel%' OR TABLE_NAME LIKE 'Hard%'";
            return await _context.Database.SqlQueryRaw<string>(sql).ToListAsync();
        }

    }
}
