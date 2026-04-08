using Microsoft.AspNetCore.Http;
using PayItOff.Application.Interfaces;

namespace PayItOff.Infrastructure.Services;

public class FileService : IFileService
{
    public async Task<string?> SaveAvatarAsync(IFormFile? avatar)
    {
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