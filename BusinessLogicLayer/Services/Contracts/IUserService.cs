using System.Security.Claims;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Identity;

namespace BusinessLogicLayer.Services.Contracts
{
    public interface IUserService
    {
        Task<string> GenerateQRCodeForUserAsync(User user);
        Task<string> GenerateSecretKeyForUserAsync(User user);
        Task<List<QuizResult>> GetQuizResultsAsync(string userId);
        Task<string> GetProfilePictureBase64Async(User user);

        Task<bool> VerifyCurrentPasswordAsync(ClaimsPrincipal principal, string currentPassword);
        Task<bool> ChangePasswordAsync(ClaimsPrincipal principal, string currentPassword, string newPassword);

    }

}