using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PayItOff.MauiClient.Services;

public class GroupService
{
    private readonly JsonSerializerOptions _options;
    private readonly HttpClient _httpClient;

    public GroupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<GroupDetailsResponse?> GetGroupDetails(int groupId)
    {
        var response = await _httpClient.GetAsync($"Group/{groupId}/details");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GroupDetailsResponse>(_options);
        }
        return null;
    }

    public async Task<List<GroupInfoResponse>> GetUserGroups()
    {
        var response = await _httpClient.GetAsync("Group/groups");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<GroupInfoResponse>>(_options) ?? []
            : [];
    }

    public async Task<bool> CreateGroup(string groupName, Stream? avatarStream = null, string? fileName = null)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(groupName), "Name");

        if (avatarStream != null && !string.IsNullOrEmpty(fileName))
        {
            var imageContent = new StreamContent(avatarStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "avatar", fileName);
        }

        var response = await _httpClient.PostAsync("Group/create", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetGroupFavorite(int groupId)
    {
        var response = await _httpClient.PatchAsync($"GroupMember/{groupId}/set-fav", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ActiveGroupsDisplayResponse>> Get4ActiveGroups()
    {
        var response = await _httpClient.GetAsync("Group/last-active-groups");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<ActiveGroupsDisplayResponse>>(_options) ?? []
            : [];
    }

    public async Task<bool> EditGroupInfoAsync(EditGroupInfoRequest request, Stream? avatarStream = null, string? fileName = null)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.GroupId.ToString()), "GroupId");
        if (!string.IsNullOrEmpty(request.NewName))
        {
            content.Add(new StringContent(request.NewName), "NewName");
        }

        if (avatarStream != null && !string.IsNullOrEmpty(fileName))
        {
            var imageContent = new StreamContent(avatarStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "avatar", fileName);
        }

        var response = await _httpClient.PatchAsync("Group/group-edit", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        var requestMsg = new HttpRequestMessage(HttpMethod.Delete, "Group/group-delete")
        {
            Content = JsonContent.Create(new DeleteGroupRequest { GroupId = groupId })
        };
        var response = await _httpClient.SendAsync(requestMsg);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<GroupInfoResponse>> GetArchivedGroups()
    {
        var response = await _httpClient.GetAsync("Group/archived");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<GroupInfoResponse>>(_options) ?? []
            : [];
    }

    public async Task<List<AuditLogResponse>> GetGroupHistory(int groupId)
    {
        var response = await _httpClient.GetAsync($"Group/{groupId}/history");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>(_options) ?? []
            : [];
    }
}