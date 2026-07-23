using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"User/login", request);

        if (response.IsSuccessStatusCode)
        {

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (!string.IsNullOrEmpty(result!.RefreshToken))
            {
                await SecureStorage.Default.SetAsync("refresh_token", result.RefreshToken);
            }

            await SecureStorage.Default.SetAsync("jwt_token", result!.Token);

            var userId = Helpers.JwtHelper.GetClaimValue(result.Token, "nameid");
            if (!string.IsNullOrEmpty(userId))
            {
                await SecureStorage.Default.SetAsync("user_id", userId);
            }

            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            try
            {

                var json = JsonDocument.Parse(errorContent);
                var errorMessage = json.RootElement.GetProperty("Error").GetString();
                throw new Exception(errorMessage);
            }
            catch (KeyNotFoundException)
            {

                throw new Exception("Wystąpił nieznany błąd serwera.");
            }
        }
    }


    public async Task<bool> RefreshTokensAsync()
    {
        var oldAccessToken = await SecureStorage.Default.GetAsync("jwt_token");
        var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");

        if (string.IsNullOrEmpty(oldAccessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        var request = new RefreshRequest
        {
            AccessToken = oldAccessToken,
            RefreshToken = refreshToken
        };

        var response = await _httpClient.PostAsJsonAsync("User/refresh", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            await SecureStorage.Default.SetAsync("jwt_token", result!.Token);
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                await SecureStorage.Default.SetAsync("refresh_token", result.RefreshToken);
            }

            return true;
        }

        return false;
    }
}