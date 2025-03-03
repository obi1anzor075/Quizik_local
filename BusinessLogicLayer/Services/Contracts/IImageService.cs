using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Contracts;

public interface IImageService
{
    Task<byte[]> ResizeImageIfNecessaryAsync(byte[] imageBytes, int maxWidth, int maxHeight);
    Task<byte[]> ProcessImageAsync(IFormFile imageFike);
    Task<(byte[] ImageData, string FileName)> GetRandomProfilePictureAsync();

    string DecodeImageAsync(byte[] imageData, string contentType = "image/jpeg");

}
