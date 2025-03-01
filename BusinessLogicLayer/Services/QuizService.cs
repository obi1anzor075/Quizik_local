using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class QuizService :IQuizService
    {
        private readonly IQuizRepository _quizRepository;

        public QuizService(IQuizRepository quizResultRepository)
        {
            _quizRepository = quizResultRepository;
        }

        public async Task<IEnumerable<QuizResult>> GetQuizResultsByUserAsync(string userId)
        {
            return await _quizRepository.GetQuizResultsByUserIdAsync(userId);
        }

        // Получение всех простых вопросов
        public async Task<IEnumerable<Question>> GetAllQuestionsAsync()
        {
            return await _quizRepository.GetAllQuestionsAsync();
        }

        // Получение всех сложных вопросов
        public async Task<IEnumerable<HardQuestion>> GetAllHardQuestionsAsync()
        {
            return await _quizRepository.GetAllHardQuestionsAsync();
        }

        // Добавление простого/дуэльного вопроса в таблицу
        public async Task AddQuestionAsync(Question question, string tableName)
        {
            await _quizRepository.AddQuestionAsync(question, tableName);
        }

        // Добавление сложного вопроса в таблицу
        public async Task AddHardQuestionAsync(HardQuestion hardQuestion, string tableNAme)
        {
            await _quizRepository.AddHardQuestionAsync(hardQuestion, tableNAme);
        }
    }
}
