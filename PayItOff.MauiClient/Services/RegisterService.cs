using System.Net.Http.Json;
using System.Text.Json;
using PayItOff.Shared.Requests;

namespace PayItOff.MauiClient.Services;

public class RegisterService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5180/api/User"; // Zmień na swój port!

    public RegisterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", request);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var errorContent = await response.Content.ReadAsStringAsync();

        throw new Exception($"Błąd serwera: {errorContent}");
    }
}