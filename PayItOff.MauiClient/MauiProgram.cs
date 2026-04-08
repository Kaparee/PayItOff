using Microsoft.Extensions.Logging;
using PayItOff.MauiClient.Views;
using PayItOff.MauiClient.Services;
using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<HttpClient>();
            // Services
            builder.Services.AddTransient<AuthHandler>();
            builder.Services.AddSingleton<RegisterService>();
            builder.Services.AddHttpClient<AuthService>();

            builder.Services.AddHttpClient<AuthService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5180/api/");
            })
            .AddHttpMessageHandler<AuthHandler>();

            //ViewModels
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<LoginViewModel>();

            //Views
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<LoginPage>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
