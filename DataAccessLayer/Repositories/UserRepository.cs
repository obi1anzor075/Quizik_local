using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace DataAccessLayer.Repositories
{
    public class UserRepository :  IUserRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;


        public UserRepository(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<User> GetUserAsync(ClaimsPrincipal principal)
        {
            return await _userManager.GetUserAsync(principal);
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.ToLower(); // Приводим email к нижнему регистру
            return await _userManager.FindByEmailAsync(normalizedEmail);
        }

        public async Task<string> GetAuthenticatorKeyAsync(User user)
        {
            return await _userManager.GetAuthenticatorKeyAsync(user);
        }

        public async Task<string> ResetAuthenticatorKeyAsync(User user)
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            return await _userManager.GetAuthenticatorKeyAsync(user);
        }

        public async Task<IdentityResult> UpdateAsync(User user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<bool> CheckPasswordAsync(User user, string currentPassword)
        {
            return await _userManager.CheckPasswordAsync(user, currentPassword);
        }

        public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword)
        {
            var passwordValid = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!passwordValid)
            {
                return false; // Текущий пароль неверный
            }

            var result =  await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }

    }
}
