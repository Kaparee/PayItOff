using Microsoft.AspNetCore.Http;

namespace PayItOff.Application.Interfaces;

public interface IFileService
{
    Task<string?> SaveAvatarAsync(IFormFile? file);
    Task<string?> SaveFileAsync(IFormFile? file);
    void DeleteFile(string fileName);
}