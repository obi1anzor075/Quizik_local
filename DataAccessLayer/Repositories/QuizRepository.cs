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
    }
}
