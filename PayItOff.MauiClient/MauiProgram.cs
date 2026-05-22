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
            builder.Services.AddScoped<SettlementService>();
            builder.Services.AddScoped<GroupService>();
            builder.Services.AddScoped<FriendService>();
            builder.Services.AddScoped<UserService>();

            // ViewModels
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<AccountsViewModel>();
            builder.Services.AddTransient<ArchiveViewModel>();
            builder.Services.AddTransient<FriendsViewModel>();
            builder.Services.AddTransient<GroupsViewModel>();
            builder.Services.AddTransient<NotificationsViewModel>();
            builder.Services.AddTransient<WalletViewModel>();
            builder.Services.AddTransient<GroupDetailsViewModel>();

            // Views
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<AccountPage>();
            builder.Services.AddTransient<ArchivePage>();
            builder.Services.AddTransient<FriendsPage>();
            builder.Services.AddTransient<GroupsPage>();
            builder.Services.AddTransient<NotificationsPage>();
            builder.Services.AddTransient<WalletPage>();
            builder.Services.AddTransient<GroupDetailsPage>();

            return builder.Build();
        }
    }
}
