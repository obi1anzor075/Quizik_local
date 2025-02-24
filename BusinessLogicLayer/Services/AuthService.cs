using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ActionResult> Enable2FAAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return new BadRequestObjectResult("User not found.");

            var secretKey = await _userRepository.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(secretKey))
            {
                secretKey = await _userRepository.ResetAuthenticatorKeyAsync(user);
            }

            string qrCodeUrl = $"otpauth://totp/Quizik:{user.UserName}?secret={secretKey}&issuer=QuizApp&digits=6";
            var qrCodeImageUrl = GenerateQrCodeForAuthenticator(qrCodeUrl);

            return new OkObjectResult(new { QrCodeImageUrl = qrCodeImageUrl, SecretKey = secretKey });
        }

        public async Task<ActionResult> Disable2FAAsync(ClaimsPrincipal principal)
        {
            var user = await _userRepository.GetUserAsync(principal);
            if (user == null)
            {
                return new BadRequestObjectResult("User not found.");
            }

            // Отключаем 2FA
            user.TwoFactorEnabled = false;
            var result = await _userRepository.UpdateAsync(user);
            if (result.Succeeded)
            {
                return new OkObjectResult("Two-factor authentication disabled.");
            }
            else
            {
                return new BadRequestObjectResult("Error disabling 2FA.");
            }
        }

        // Метод генерации QR-кода
        public string GenerateQrCodeForAuthenticator(string setupCode)
        {
            // Создаём генератор QR-кода
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(setupCode, QRCodeGenerator.ECCLevel.Q);

            // Генерируем QR-код в формате PNG
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            // Преобразуем изображение в Base64 для вывода в браузере
            string base64Image = Convert.ToBase64String(qrCodeImage);
            return $"data:image/png;base64,{base64Image}";
        }
    }



}
