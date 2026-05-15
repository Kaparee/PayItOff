using PayItOff.MauiClient.Views;

namespace PayItOff.MauiClient
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
            Routing.RegisterRoute(nameof(GroupsPage), typeof(GroupsPage));
            Routing.RegisterRoute(nameof(FriendsPage), typeof(FriendsPage));
            Routing.RegisterRoute(nameof(AccountPage), typeof(AccountPage));
            Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));
            Routing.RegisterRoute(nameof(ArchivePage), typeof(ArchivePage));
            Routing.RegisterRoute(nameof(GroupDetailsPage), typeof(GroupDetailsPage));
        }
    }
}
