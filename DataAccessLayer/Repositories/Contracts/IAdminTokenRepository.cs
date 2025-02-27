using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Contracts
{
    public interface IAdminTokenRepository
    {
        Task<AdminRegistrationToken> GenerateTokenAsync();
        Task<bool> ValidateTokenAsync(string token);
        Task InvalidateTokenAsync(string token);
    }
}
