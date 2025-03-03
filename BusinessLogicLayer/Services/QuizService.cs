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
        public async Task AddQuestionAsync(QuestionDTO model)
        {
            if (model.TableName.StartsWith("Hard", StringComparison.OrdinalIgnoreCase))
            {
                var hardQuestion = new HardQuestion
                {
                    QuestionText = model.QuestionText,
                    ImageData = model.ImageData,
                    CorrectAnswer = model.CorrectAnswer,
                    CorrectAnswer2 = model.CorrectAnswer2
                };
                await _quizRepository.AddHardQuestionAsync(hardQuestion, model.TableName);
            }
            else if (model.TableName.StartsWith("Easy", StringComparison.OrdinalIgnoreCase) ||
                     model.TableName.StartsWith("Duel", StringComparison.OrdinalIgnoreCase))
            {
                var question = new Question
                {
                    QuestionText = model.QuestionText,
                    ImageData = model.ImageData,
                    CorrectAnswer = model.CorrectAnswer,
                    Answer1 = model.Answer1,
                    Answer2 = model.Answer2,
                    Answer3 = model.Answer3,
                    Answer4 = model.Answer4,
                    QuestionExplanation = string.Empty
                };
                await _quizRepository.AddQuestionAsync(question, model.TableName);
            }
            else
            {
                throw new ArgumentException("Неверное имя таблицы!");
            }
        }

        // Добавление сложного вопроса в таблицу
        public async Task AddHardQuestionAsync(HardQuestion hardQuestion, string tableNAme)
        {
            await _quizRepository.AddHardQuestionAsync(hardQuestion, tableNAme);
        }

        public async Task<IEnumerable<string>> GetTableNamesAsync()
        {
            return await _quizRepository.GetTableNamesAsync();
        }
    }
}
