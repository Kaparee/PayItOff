using System.Text;
using System.Text.Json;

namespace PayItOff.MauiClient.Helpers;

public static class JwtHelper
{
    public static string? GetClaimValue(string token, string claimType)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            // Fix Base64 padding
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(claimType, out var valueElement))
            {
                return valueElement.GetString();
            }

            // Fallback for standard ClaimTypes.NameIdentifier
            var standardNameIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            if (claimType == "nameid" || claimType == standardNameIdClaim)
            {
                if (doc.RootElement.TryGetProperty("nameid", out var nameIdElement))
                {
                    return nameIdElement.GetString();
                }
                if (doc.RootElement.TryGetProperty(standardNameIdClaim, out var longNameIdElement))
                {
                    return longNameIdElement.GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
