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

    public async Task<List<GroupInfoResponse>> GetUserGroups()
    {
        var response = await _httpClient.GetAsync("Group/groups");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<GroupInfoResponse>>(_options) ?? [];
        }

        return [];
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

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ActiveGroupsDisplayResponse>>(_options) ?? [];
        }

        return [];
    }
}