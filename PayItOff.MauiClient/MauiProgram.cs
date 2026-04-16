using PayItOff.MauiClient.Services;
using PayItOff.MauiClient.ViewModels;
using PayItOff.MauiClient.Views;

namespace PayItOff.MauiClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5180/api/"
                : "http://localhost:5180/api/";


            builder.Services.AddTransient<AuthHandler>();

            builder.Services.AddHttpClient("PayItOffApi", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            })
            .AddHttpMessageHandler<AuthHandler>();

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("PayItOffApi"));

            // Services
            builder.Services.AddSingleton<RegisterService>();
            builder.Services.AddScoped<AuthService>();

            // ViewModels
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<LoginViewModel>();

            // Views
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<LoginPage>();

            return builder.Build();
        }
    }
}
