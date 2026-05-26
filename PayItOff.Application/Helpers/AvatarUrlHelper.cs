namespace PayItOff.Application.Helpers;

public static class AvatarUrlHelper
{
    public static string BuildUserAvatarUrl(string baseUrl, string? avatarUrl)
    {
        return avatarUrl != null
            ? $"{baseUrl}/avatars/{avatarUrl}"
            : $"{baseUrl}/avatars/default-user-avatar.png";
    }

    public static string BuildGroupAvatarUrl(string baseUrl, string? avatarUrl)
    {
        return avatarUrl != null
            ? $"{baseUrl}/avatars/{avatarUrl}"
            : $"{baseUrl}/avatars/default-group-avatar.png";
    }
}
