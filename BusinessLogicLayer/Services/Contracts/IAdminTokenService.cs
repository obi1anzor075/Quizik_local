using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Contracts
{
    public interface IAdminTokenService
    {
        Task<string> GenerateTokenAsync();
        Task<bool> ValidateTokenAsync(string token);
        Task InvalidateTokenAsync(string token);
    }
}
