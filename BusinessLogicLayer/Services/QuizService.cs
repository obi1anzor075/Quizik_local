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
    }
}
