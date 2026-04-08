using System.Net.Http.Json;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5180/api/User";

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            await SecureStorage.Default.SetAsync("jwt_token", result!.Token);
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            try
            {
                // Wyciągamy samą wiadomość z JSON-a
                var json = JsonDocument.Parse(errorContent);
                var errorMessage = json.RootElement.GetProperty("Error").GetString();
                throw new Exception(errorMessage);
            }
            catch (KeyNotFoundException)
            {
                // Jeśli serwer rzuci inny błąd (bez pola "Error"), łapiemy to bezpiecznie
                throw new Exception("Wystąpił nieznany błąd serwera.");
            }
        }
    }
}