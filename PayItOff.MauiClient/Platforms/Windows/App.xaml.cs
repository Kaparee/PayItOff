// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PayItOff.MauiClient.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "maui_crash.txt");
                    System.IO.File.AppendAllText(path, $"[{DateTime.Now}] UNHANDLED EXCEPTION: {ex}\n");
                }
                catch { }
            };

            Microsoft.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
            {
                try
                {
                    var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "maui_crash.txt");
                    System.IO.File.AppendAllText(path, $"[{DateTime.Now}] WINUI EXCEPTION: {e.Exception}\n{e.Message}\n");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try
                {
                    var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "maui_crash.txt");
                    System.IO.File.AppendAllText(path, $"[{DateTime.Now}] TASK EXCEPTION: {e.Exception}\n");
                }
                catch { }
            };

            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
