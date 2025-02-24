using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Repositories.Contracts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using QRCoder;

namespace DataAccessLayer.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly string _imageFolderPath;

        public ImageRepository(string imageFolderPath)
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
