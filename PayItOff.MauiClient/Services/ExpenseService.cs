using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Net.Http.Json;

namespace PayItOff.MauiClient.Services;

public class ExpenseService
{
    private readonly HttpClient _httpClient;

    public ExpenseService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("PayItOffApi");
    }

    public async Task CreateExpenseBatch(CreateExpenseBatchRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("Expense/create", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> UploadReceiptAsync(FileResult file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = await file.OpenReadAsync();
        content.Add(new StreamContent(stream), "file", file.FileName);

        var response = await _httpClient.PostAsync("Expense/upload-receipt", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
        return result?["fileName"]?.ToString() ?? string.Empty;
    }

    public async Task<ExpenseDetailsResponse?> GetExpenseDetailsAsync(int expenseId)
    {
        return await _httpClient.GetFromJsonAsync<ExpenseDetailsResponse>($"Expense/{expenseId}");
    }

    public async Task<ExpenseDetailsResponse?> GetExpenseItemDetailsAsync(int expenseId, int itemId)
    {
        return await _httpClient.GetFromJsonAsync<ExpenseDetailsResponse>($"Expense/{expenseId}/item/{itemId}");
    }

    public async Task UpdateExpenseItemAsync(int expenseId, int itemId, PayItOff.Shared.Requests.UpdateExpenseItemRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"Expense/{expenseId}/item/{itemId}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteExpenseItemAsync(int expenseId, int itemId)
    {
        var response = await _httpClient.DeleteAsync($"Expense/{expenseId}/item/{itemId}");
        response.EnsureSuccessStatusCode();
    }
}
