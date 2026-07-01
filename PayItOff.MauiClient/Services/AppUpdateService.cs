namespace PayItOff.MauiClient.Services;

public sealed class AppUpdateInfo
{
    internal object? UpdateHandle { get; init; }
    public string Version { get; init; } = string.Empty;
}

public sealed class AppUpdateService
{
    private const string UpdateUrl = "https://payitoffupdates.blob.core.windows.net/updates";
    private const string SkippedUpdateKey = "skipped_update_version";

    public async Task<AppUpdateInfo?> CheckForUpdateAsync()
    {
#if WINDOWS
        var mgr = new Velopack.UpdateManager(UpdateUrl);
        if (!mgr.IsInstalled)
            return null;

        var updateInfo = await mgr.CheckForUpdatesAsync();
        if (updateInfo == null)
            return null;

        var version = updateInfo.TargetFullRelease.Version.ToString();
        if (Preferences.Default.Get(SkippedUpdateKey, string.Empty) == version)
            return null;

        return new AppUpdateInfo
        {
            UpdateHandle = new WindowsUpdateHandle(mgr, updateInfo),
            Version = version
        };
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    public void SkipVersion(string version) =>
        Preferences.Default.Set(SkippedUpdateKey, version);

    public async Task DownloadAndRestartAsync(AppUpdateInfo update)
    {
#if WINDOWS
        if (update.UpdateHandle is not WindowsUpdateHandle handle)
            return;

        await handle.Manager.DownloadUpdatesAsync(handle.UpdateInfo);
        handle.Manager.ApplyUpdatesAndRestart(handle.UpdateInfo);
#else
        await Task.CompletedTask;
#endif
    }

#if WINDOWS
    private sealed class WindowsUpdateHandle(Velopack.UpdateManager manager, Velopack.UpdateInfo updateInfo)
    {
        public Velopack.UpdateManager Manager { get; } = manager;
        public Velopack.UpdateInfo UpdateInfo { get; } = updateInfo;
    }
#endif
}
