using Microsoft.AspNetCore.Http;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Exceptions;

namespace PayItOff.Infrastructure.Services;

public class FileService : IFileService
{
    public async Task<string?> SaveAvatarAsync(IFormFile? avatar)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new Exception("Niedozwolony format pliku. Obsługiwane to: JPG, PNG, WEBP.");
        }

        if (avatar.Length > 5 * 1024 * 1024)
        {
            throw new Exception("Plik jest za duży. Maksymalny rozmiar to 5 MB.");
        }

        if (avatar == null || avatar.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + avatar.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await avatar.CopyToAsync(fileStream);
        }

        return uniqueFileName;
    }
}