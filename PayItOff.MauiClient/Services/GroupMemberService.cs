using System.Net.Http.Json;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.MauiClient.Services;

public class GroupMemberService
{
    private readonly HttpClient _httpClient;

    public GroupMemberService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GroupPendingInvitationResponse>?> GetPendingInvitationsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<GroupPendingInvitationResponse>>("GroupMember/all-pending-invitation");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<GroupMemberResponse>?> GetAllActiveGroupMembersAsync(int groupId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<GroupMemberResponse>>($"GroupMember/{groupId}/all-group-members");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> InviteUserAsync(GroupInviteUserRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("GroupMember/invite", request);
        if (response.IsSuccessStatusCode)
            return (true, string.Empty);
            
        var error = await response.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "Nie udało się wysłać zaproszenia." : error);
    }

    public async Task<bool> AcceptInviteAsync(int invitationId)
    {
        var requestMsg = new HttpRequestMessage(new HttpMethod("PATCH"), $"GroupMember/accept?invitationId={invitationId}");
        var response = await _httpClient.SendAsync(requestMsg);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeclineInviteAsync(int invitationId)
    {
        var requestMsg = new HttpRequestMessage(new HttpMethod("PATCH"), $"GroupMember/decline?invitationId={invitationId}");
        var response = await _httpClient.SendAsync(requestMsg);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateRoleAsync(GroupMemberUpdateRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync("GroupMember/update-role", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LeaveGroupAsync(int groupId)
    {
        var response = await _httpClient.DeleteAsync($"GroupMember/{groupId}/leave");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> KickUserFromGroupAsync(int groupId, int targetUserId)
    {
        var response = await _httpClient.DeleteAsync($"GroupMember/{groupId}/kick/{targetUserId}");
        return response.IsSuccessStatusCode;
    }
}
