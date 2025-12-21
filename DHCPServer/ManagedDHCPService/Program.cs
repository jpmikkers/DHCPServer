using ManagedDHCPService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

// see https://consultwithgriff.com/building-window-services-in-dotnet/
var builder = Host.CreateApplicationBuilder(args);

if(OperatingSystem.IsWindows())
{
    // this will set up the Windows Service lifetime and logging to the event log
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Managed DHCP Service";
    });

    // See: https://github.com/dotnet/runtime/issues/47303
    LoggerProviderOptions.RegisterProviderOptions<
        EventLogSettings, EventLogLoggerProvider>(builder.Services);

    builder.Logging.AddConfiguration(
        builder.Configuration.GetSection("Logging"));
}
else if(OperatingSystem.IsLinux())
{
    // this will set up the systemd lifetime and logging to the systemd journal
    builder.Services.AddSystemd();
    // see: https://blog.maartenballiauw.be/post/2021/05/25/running-a-net-application-as-a-service-on-linux-with-systemd.html
    // see: https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux
}

builder.Services.AddSingleton<Worker>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
