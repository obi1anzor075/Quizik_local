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
    public class AdminTokenRepository : IAdminTokenRepository
    {
        private readonly DataStoreDbContext _context;

        public AdminTokenRepository(DataStoreDbContext context)
        {
            _context = context;
        }

        public async Task<AdminRegistrationToken> GenerateTokenAsync()
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "").Replace("+", ""); // Убираем недопустимые символы

            var newToken = new AdminRegistrationToken
            {
                Token = token,
                ExpireDate = DateTime.UtcNow.AddHours(1) // Время действия токена
            };

            _context.AdminRegistrationTokens.Add(newToken);
            await _context.SaveChangesAsync();
            return newToken;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var existingToken = await _context.AdminRegistrationTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpireDate > DateTime.UtcNow);

            return existingToken != null;
        }

        public async Task InvalidateTokenAsync(string token)
        {
            var existingToken = await _context.AdminRegistrationTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (existingToken != null)
            {
                existingToken.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
