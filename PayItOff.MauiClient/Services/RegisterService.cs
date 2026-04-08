using System.Net.Http.Json;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.Services;

public class RegisterService
{
    private readonly HttpClient _httpClient;

    public RegisterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<bool> RegisterAsync(RegisterRequest request, FileResult? avatarFile)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(request.Email ?? ""), "Email");
        content.Add(new StringContent(request.Password ?? ""), "Password");
        content.Add(new StringContent(request.Nickname ?? ""), "Nickname");
        content.Add(new StringContent(request.Name ?? ""), "Name");
        content.Add(new StringContent(request.Surname ?? ""), "Surname");

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            content.Add(new StringContent(request.PhoneNumber), "PhoneNumber");

        if (!string.IsNullOrWhiteSpace(request.IBAN))
            content.Add(new StringContent(request.IBAN), "IBAN");

        if (avatarFile != null)
        {
            var fileStream = await avatarFile.OpenReadAsync();
            var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "avatar", avatarFile.FileName);
        }

        var response = await _httpClient.PostAsync($"User/register", content);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Błąd serwera: {errorContent}");
    }
}