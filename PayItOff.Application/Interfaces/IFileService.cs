using Microsoft.AspNetCore.Http;

namespace PayItOff.Application.Interfaces;

public interface IFileService
{

    Task<string?> SaveAvatarAsync(IFormFile? avatar);
}