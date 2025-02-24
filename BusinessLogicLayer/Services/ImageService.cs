﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Repositories.Contracts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using QRCoder;
using BusinessLogicLayer.Services.Contracts;

namespace BusinessLogicLayer.Services;

public class ImageService : IImageService
{
    private readonly string _imageFolderPath;

    public ImageService(string imageFolderPath)
    {
        _imageFolderPath = imageFolderPath;
    }

    public async Task<byte[]> ResizeImageIfNecessaryAsync(byte[] imageBytes, int maxWidth, int maxHeight)
    {
        using var imageWithFormat = SixLabors.ImageSharp.Image.Load(imageBytes);
        var image = imageWithFormat;
        var format = imageWithFormat.Metadata.DecodedImageFormat;

        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new SixLabors.ImageSharp.Size(maxWidth, maxHeight)
            }));
        }

        using MemoryStream ms = new MemoryStream();
        await image.SaveAsync(ms, format);
        return ms.ToArray();
    }

    public async Task<byte[]> ProcessImageAsync(IFormFile avatar)
    {
        using MemoryStream ms = new MemoryStream();
        await avatar.CopyToAsync(ms);
        return ms.ToArray();
    }

    // Асинхронный метод для получения случайной картинки
    public async Task<(byte[] ImageData, string FileName)> GetRandomProfilePictureAsync()
    {
        var images = Directory.GetFiles(_imageFolderPath);
        if (images.Length == 0)
            return (null, null);

        var random = new Random();
        var randomImagePath = images[random.Next(images.Length)];
        var imageData = await File.ReadAllBytesAsync(randomImagePath);
        var fileName = Path.GetFileName(randomImagePath); // Извлекаем только имя файла

        return (imageData, fileName);
    }

}
