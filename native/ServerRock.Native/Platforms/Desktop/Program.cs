using Uno.UI.Hosting;

namespace ServerRock.Native;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var singleInstance = new Mutex(true, "ServerRock.AI.Native.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance) return;
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
