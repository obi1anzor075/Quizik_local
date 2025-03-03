using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Contracts
{
    public interface IQuizRepository
    {
        Task<IEnumerable<QuizResult>> GetQuizResultsByUserIdAsync(string userId);
        Task<IEnumerable<Question>> GetAllQuestionsAsync();
        Task<IEnumerable<HardQuestion>> GetAllHardQuestionsAsync();
        Task AddQuestionAsync(Question question, string tableName);
        Task AddHardQuestionAsync(HardQuestion hardQuestion, string tableName);
        Task<IEnumerable<string>> GetTableNamesAsync();

    }
}
