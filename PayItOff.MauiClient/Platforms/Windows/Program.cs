using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace PayItOff.MauiClient.WinUI;

public static class Program
{
    [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {
        // 1. VELOPACK INITIALIZATION - MUST BE FIRST
        Velopack.VelopackApp.Build().Run();

        // 2. STANDARD WINUI3 MAUI INITIALIZATION
        XamlCheckProcessRequirements();
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) => {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
