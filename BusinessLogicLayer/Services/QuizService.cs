using BusinessLogicLayer.DTO;
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

        private readonly IImageService _imageService;

        public QuizService(IQuizRepository quizResultRepository, IImageService imageService)
        {
            _quizRepository = quizResultRepository;
            _imageService = imageService;
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
        public async Task AddQuestionAsync(Question question)
        {
            await _quizRepository.AddQuestionAsync(question);
        }

        // Добавление сложного вопроса в таблицу
        public async Task AddHardQuestionAsync(HardQuestion hardQuestion)
        {
            await _quizRepository.AddHardQuestionAsync(hardQuestion);
        }

        public async Task<IEnumerable<string>> GetTableNamesAsync()
        {
            return await _quizRepository.GetTableNamesAsync();
        }
    }
}
