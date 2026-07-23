namespace PayItOff.Application.Helpers;

public static class UrlHelper
{
    public static string BuildUserAvatarUrl(string baseUrl, string? avatarUrl)
    {
        if (avatarUrl is "default-avatar.png" or "default-user-avatar.png")
        {
            avatarUrl = "default_user_avatar.png";
        }

        return avatarUrl != null
            ? $"{baseUrl}/avatars/{avatarUrl}"
            : $"{baseUrl}/avatars/default_user_avatar.png";
    }

    public static string BuildGroupAvatarUrl(string baseUrl, string? avatarUrl)
    {
        if (avatarUrl == "default-group-avatar.png")
        {
            avatarUrl = "default_group_avatar.png";
        }

        return avatarUrl != null
            ? $"{baseUrl}/avatars/{avatarUrl}"
            : $"{baseUrl}/avatars/default_group_avatar.png";
    }

    public static string BuildFileUrl(string baseUrl, string? fileUrl)
    {
        return fileUrl != null
            ? $"{baseUrl}/files/{fileUrl}"
            : string.Empty;
    }
}
