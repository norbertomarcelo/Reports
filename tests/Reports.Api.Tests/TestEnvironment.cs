using System.Runtime.CompilerServices;

namespace Reports.Api.Tests;

internal static class TestEnvironment
{
    /// <summary>
    /// Cada host de teste observa o appsettings via inotify, e o Linux limita as instâncias por usuário
    /// (128 por padrão, divididas com o editor e outros programas). O polling não usa inotify.
    /// </summary>
    [ModuleInitializer]
    internal static void UsePollingFileWatcher() =>
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
}
