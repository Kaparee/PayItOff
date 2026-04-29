using PayItOff.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class SettlementService
{
    private readonly JsonSerializerOptions _options;
    private readonly HttpClient _httpClient;
    public SettlementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<GlobalSettlementResponse> GetIncomesAsync()
    {
        var response = await _httpClient.GetAsync("Settlement/get-user-incomes-summ");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GlobalSettlementResponse>(_options) ?? new();
        }
        return new();
    }

    public async Task<GlobalSettlementResponse> GetExpensesAsync()
    {
        var response = await _httpClient.GetAsync("Settlement/get-user-expenses-summ");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GlobalSettlementResponse>(_options) ?? new();
        }
        return new();
    }
}