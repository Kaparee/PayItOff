using PayItOff.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class NotificationService
{
    private readonly JsonSerializerOptions _options;
    private readonly HttpClient _httpClient;

    public NotificationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<NotificationResponse>> Get5LastNotification()
    {
        var response = await _httpClient.GetAsync("Notification/get-last-5-notifications");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(_options) ?? []
            : [];
    }

    public async Task<List<NotificationResponse>> GetAllNotifications(string? type1 = null, string? type2 = null)
    {
        var query = "Notification/get-all-notifications";
        if (!string.IsNullOrEmpty(type1) && !string.IsNullOrEmpty(type2)) query += $"?type1={type1}&type2={type2}";
        else if (!string.IsNullOrEmpty(type1)) query += $"?type1={type1}";
        else if (!string.IsNullOrEmpty(type2)) query += $"?type1={type2}";

        var response = await _httpClient.GetAsync(query);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(_options) ?? []
            : [];
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        await _httpClient.PatchAsync($"Notification/set-as-read?notificationId={notificationId}", null);
    }

    public async Task MarkAllAsReadAsync()
    {
        await _httpClient.PatchAsync("Notification/set-all-as-read", null);
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        await _httpClient.DeleteAsync($"Notification/delete?notificationId={notificationId}");
    }

    public async Task DeleteAllNotificationsAsync()
    {
        await _httpClient.DeleteAsync("Notification/delete-all");
    }
}