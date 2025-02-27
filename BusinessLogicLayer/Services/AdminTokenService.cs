using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Repositories.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class AdminTokenService : IAdminTokenService
    {
        private readonly IAdminTokenRepository _adminTokenRepository;

        public AdminTokenService(IAdminTokenRepository adminTokenRepository)
        {
            _adminTokenRepository = adminTokenRepository;
        }

        public async Task<string> GenerateTokenAsync()
        {
            var token = await _adminTokenRepository.GenerateTokenAsync();
            return token.Token;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return await _adminTokenRepository.ValidateTokenAsync(token);
        }

        public async Task InvalidateTokenAsync(string token)
        {
            await _adminTokenRepository.InvalidateTokenAsync(token);
        }
    }
}
