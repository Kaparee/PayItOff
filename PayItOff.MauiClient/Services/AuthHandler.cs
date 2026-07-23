using System.Net;

namespace PayItOff.MauiClient.Services;

public class AuthHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;

    public AuthHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("refresh", StringComparison.OrdinalIgnoreCase))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var token = await SecureStorage.Default.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var authService = _serviceProvider.GetRequiredService<AuthService>();

            bool refreshed = await authService.RefreshTokensAsync();

            if (refreshed)
            {
                var newToken = await SecureStorage.Default.GetAsync("jwt_token");

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);

                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                SecureStorage.Default.Remove("jwt_token");
                SecureStorage.Default.Remove("refresh_token");
                SecureStorage.Default.Remove("user_id");

                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        return response;
    }
}