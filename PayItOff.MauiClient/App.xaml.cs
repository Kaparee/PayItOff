namespace PayItOff.MauiClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell()) { Title = "PayItOff!" };

            window.Created += (s, e) =>
            {
#if WINDOWS
                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window.Handler.PlatformView);
                var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                appWindow.Title = "PayItOff!";
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                {
                    p.Maximize();
                }
#endif
            };

            return window;
        }
    }
}
