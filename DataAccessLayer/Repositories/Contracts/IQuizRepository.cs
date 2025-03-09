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
        Task AddQuestionAsync(Question question);
        Task AddHardQuestionAsync(HardQuestion hardQuestion);
        Task<IEnumerable<string>> GetTableNamesAsync();

    }
}
