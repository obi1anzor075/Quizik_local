using System.Security.Claims;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PresentationLayer.ErrorDescriber;
using QRCoder;

namespace BusinessLogicLayer.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly DataStoreDbContext _context;

        private readonly IUserRepository _userRepository;

        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IFileProvider _fileProvider; // Для работы с изображениями

        public UserService(UserManager<User> userManager, DataStoreDbContext context, IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IFileProvider fileProvider)
        {
            _userManager = userManager;
            _context = context;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _fileProvider = fileProvider;
        }

        public async Task<string> GenerateQRCodeForUserAsync(User user)
        {
            var secretKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(secretKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                secretKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            string qrCodeUrl = $"otpauth://totp/QuizApp:{user.UserName}?secret={secretKey}&issuer=QuizApp&digits=6";
            return GenerateQrCode(qrCodeUrl);
        }
        public async Task<string> GenerateSecretKeyForUserAsync(User user)
        {
            var secretKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(secretKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                secretKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return secretKey;
        }
        public async Task<List<QuizResult>> GetQuizResultsAsync(string userId)
        {
            return await _context.QuizResults
                                 .Where(qr => qr.UserId == userId)
                                 .OrderByDescending(qr => qr.DatePlayed)
                                 .ToListAsync();
        }        
        
        public async Task<string> GetProfilePictureBase64Async(User user)
        {
            if (user.ProfilePicture != null && user.ProfilePicture.Length > 0)
            {
                var provider = new FileExtensionContentTypeProvider();
                string contentType = "application/octet-stream"; // Тип по умолчанию

                if (provider.TryGetContentType(user.ProfilePictureFileName, out string detectedContentType))
                {
                    contentType = detectedContentType;
                }

                return $"data:{contentType};base64,{Convert.ToBase64String(user.ProfilePicture)}";
            }

            return string.Empty;
        }

        public async Task<bool> VerifyCurrentPasswordAsync(ClaimsPrincipal principal, string currentPassword)
        {
            var user = await _userRepository.GetUserAsync(principal);
            if (user == null) return false;

            return await _userRepository.CheckPasswordAsync(user, currentPassword);
        }

        public async Task<bool> ChangePasswordAsync(ClaimsPrincipal principal, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetUserAsync(principal);
            if (user == null) return false;

            return await _userRepository.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        //Вспомогательные методы
        private string GenerateQrCode(string setupCode)
        {
            // Генерация QR-кода
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(setupCode, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
        }

    }

}