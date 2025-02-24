using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Contracts
{
    public interface IImageRepository
    {
        Task<byte[]> ResizeImageIfNecessaryAsync(byte[] imageBytes, int maxWidth, int maxHeight);
        Task<byte[]> ProcessImageAsync(IFormFile avatar);
        Task<(byte[] ImageData, string FileName)> GetRandomProfilePictureAsync();

        string GenerateQrCodeForAuthenticator(string setupCode);

    }
}
