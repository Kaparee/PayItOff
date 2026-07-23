using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class SettlementService
{
    private readonly HttpClient _httpClient;

    public SettlementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GlobalSettlementResponse?> GetUserAllIncomesSummaryAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GlobalSettlementResponse>("Settlement/get-user-incomes-summ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching incomes summary: {ex.Message}");
            return null;
        }
    }

    public async Task<GlobalSettlementResponse?> GetUserAllExpensesSummaryAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GlobalSettlementResponse>("Settlement/get-user-expenses-summ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching expenses summary: {ex.Message}");
            return null;
        }
    }

    public async Task<PagedTransactionResponse?> GetHistoryAsync(int page, string type, int? targetId = null)
    {
        try
        {
            var url = $"Settlement/get-user-expense-history?Page={page}&Type={type}";
            if (targetId.HasValue)
            {
                url += $"&TargetId={targetId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<PagedTransactionResponse>(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching history: {ex.Message}");
            throw new Exception("Nie udało się pobrać historii transakcji.");
        }
    }

    public async Task<decimal> GetCurrentTotalDebtAsync(int? targetId = null)
    {
        try
        {
            var url = "Settlement/current-debt";
            if (targetId.HasValue)
            {
                url += $"?targetId={targetId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<decimal>(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching current debt: {ex.Message}");
            return 0;
        }
    }

    public async Task<(bool Ok, string? Error)> CreateSettlementAsync(CreateSettlementRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("Settlement/create", request);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("Error", out var err))
                {
                    return (false, err.GetString());
                }
            }
            catch
            {
            }

            return (false, "Nie udało się utworzyć spłaty.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating settlement: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, PayNetDebtResponse? Result, string? Error)> CreateNetDebtSettlementsAsync(PayNetDebtRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("Settlement/create-net-pay", request);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<PayNetDebtResponse>();
                return (true, body, null);
            }

            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("Error", out var err))
                {
                    return (false, null, err.GetString());
                }
            }
            catch { }

            return (false, null, "Nie udało się utworzyć spłaty netto.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating net settlements: {ex.Message}");
            return (false, null, ex.Message);
        }
    }

    public async Task<List<PayableDebtOptionResponse>?> GetPayableDebtOptionsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<PayableDebtOptionResponse>>("Settlement/payable-options");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching payable options: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AcceptSettlementAsync(int settlementId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"Settlement/accept/{settlementId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting settlement: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RejectSettlementAsync(int settlementId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"Settlement/reject/{settlementId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rejecting settlement: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AcceptNetSettlementsAsync(int senderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"Settlement/accept-net/{senderId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting net settlements: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RejectNetSettlementsAsync(int senderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"Settlement/reject-net/{senderId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rejecting net settlements: {ex.Message}");
            return false;
        }
    }

    public async Task<(bool Ok, string? ErrorMessage)> CompensateDebtsAsync(CompensateDebtsRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("Settlement/compensate", request);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("Error", out var err))
                {
                    return (false, err.GetString());
                }
            }
            catch { }

            return (false, "Nie udało się rozliczyć wzajemnych długów.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error compensating debts: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string? ErrorMessage)> SendDebtReminderAsync(RemindDebtRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("Settlement/remind-debt", request);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("Error", out var err))
                {
                    return (false, err.GetString());
                }
            }
            catch { }

            return (false, "Nie udało się wysłać przypomnienia.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending debt reminder: {ex.Message}");
            return (false, ex.Message);
        }
    }
}