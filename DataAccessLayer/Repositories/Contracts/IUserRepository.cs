using DataAccessLayer.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Contracts
{
    public interface IUserRepository
    {
        Task<User> GetUserAsync(ClaimsPrincipal principal);
        Task<User> GetUserByIdAsync(string userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<string> GetAuthenticatorKeyAsync(User user);
        Task<string> ResetAuthenticatorKeyAsync(User user);
        Task<IdentityResult> UpdateAsync(User user);

        Task<bool> CheckPasswordAsync(User user, string currentPassword);
        Task<bool> ChangePasswordAsync(User user,string currentPassword, string newPassword);
    }
}
