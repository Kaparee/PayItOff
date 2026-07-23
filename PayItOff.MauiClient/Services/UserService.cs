using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class UserService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _baseUrl = httpClient.BaseAddress?.ToString() ?? string.Empty;
    }

    public string BaseUrl => _baseUrl;

    public async Task<UserInformationResponse?> GetUserInformationAsync()
    {
        var response = await _httpClient.GetAsync("User/info");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserInformationResponse>();
        }
        return null;
    }

    public async Task<bool> UpdateNotificationAsync(UserNotificationChangeRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync("User/notifications", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateInfoAsync(UserInfoUpdateRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync("User/profile", request);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception(ExtractErrorMessage(error, "Nie udało się zaktualizować danych."));
    }

    public async Task<bool> ModifyPasswordAsync(ModifyPasswordRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync("User/modify-password", request);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception(ExtractErrorMessage(error, "Nie udało się zmienić hasła."));
    }

    public async Task<bool> RequestEmailChangeAsync(string newEmail)
    {
        var request = new EmailRequest { NewEmail = newEmail };
        var response = await _httpClient.PostAsJsonAsync("User/request-email-change", request);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception(ExtractErrorMessage(error, "Nie udało się wysłać żądania zmiany email."));
    }

    public async Task<bool> DeleteAccountAsync()
    {
        var response = await _httpClient.DeleteAsync("User/delete");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAvatarAsync(FileResult file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = await file.OpenReadAsync();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "avatar", file.FileName);

        var response = await _httpClient.PostAsync("User/avatar", content);
        return response.IsSuccessStatusCode;
    }

    private string ExtractErrorMessage(string errorContent, string defaultMessage)
    {
        try
        {
            var json = JsonDocument.Parse(errorContent);
            return json.RootElement.GetProperty("Error").GetString() ?? defaultMessage;
        }
        catch
        {
            return defaultMessage;
        }
    }
}
