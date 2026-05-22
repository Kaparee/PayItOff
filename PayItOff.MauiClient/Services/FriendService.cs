using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Net.Http.Json;

namespace PayItOff.MauiClient.Services;

public class FriendService
{
    private readonly HttpClient _httpClient;

    public FriendService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<FriendListResponse>> GetUserFriendListAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("Friend/friends-list");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<FriendListResponse>>() ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd pobierania listy znajomych: {ex.Message}");
        }

        return new List<FriendListResponse>();
    }

    public async Task<List<FriendPendingInvitationResponse>> GetPendingInvitationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("Friend/all-pending-invitation");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<FriendPendingInvitationResponse>>() ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd pobierania oczekujących zaproszeń: {ex.Message}");
        }

        return new List<FriendPendingInvitationResponse>();
    }

    public async Task<bool> InviteAsync(FriendInviteRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("Friend/invite", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AcceptInviteAsync(UpdateInviteRequest request)
    {
        var requestMsg = new HttpRequestMessage(new HttpMethod("PATCH"), "Friend/accept")
        {
            Content = JsonContent.Create(request)
        };
        var response = await _httpClient.SendAsync(requestMsg);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeclineInviteAsync(UpdateInviteRequest request)
    {
        var requestMsg = new HttpRequestMessage(new HttpMethod("PATCH"), "Friend/decline")
        {
            Content = JsonContent.Create(request)
        };
        var response = await _httpClient.SendAsync(requestMsg);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveFriendAsync(UpdateInviteRequest request)
    {
        var requestMsg = new HttpRequestMessage(HttpMethod.Delete, "Friend/remove")
        {
            Content = JsonContent.Create(request)
        };
        var response = await _httpClient.SendAsync(requestMsg);
        return response.IsSuccessStatusCode;
    }

    public async Task<SearchUserResponse?> SearchUserAsync(string? nickname, string? email, string? phoneNumber)
    {
        try
        {
            var url = $"Friend/search-user?nickname={Uri.EscapeDataString(nickname ?? "")}&email={Uri.EscapeDataString(email ?? "")}&phoneNumber={Uri.EscapeDataString(phoneNumber ?? "")}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SearchUserResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd wyszukiwania użytkownika: {ex.Message}");
        }
        return null;
    }
}