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

    }

}