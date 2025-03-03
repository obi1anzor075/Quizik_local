using BusinessLogicLayer.DTO;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Contracts
{
    public interface IQuizService
    {
        Task<IEnumerable<QuizResult>> GetQuizResultsByUserAsync(string userId);
        Task<IEnumerable<Question>> GetAllQuestionsAsync();
        Task<IEnumerable<HardQuestion>> GetAllHardQuestionsAsync();
        Task AddQuestionAsync(QuestionDTO model);
        Task AddHardQuestionAsync(HardQuestion hardQuestion, string tableName);
        Task<IEnumerable<string>> GetTableNamesAsync();
    }
}
