using ManagedDHCPService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

if(OperatingSystem.IsWindows())
{
    // see https://consultwithgriff.com/building-window-services-in-dotnet/
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Managed DHCP Service";
    });

    LoggerProviderOptions.RegisterProviderOptions<
        EventLogSettings, EventLogLoggerProvider>(builder.Services);

    builder.Services.AddSingleton<Worker>();
    builder.Services.AddHostedService<Worker>();

    // See: https://github.com/dotnet/runtime/issues/47303
    builder.Logging.AddConfiguration(
        builder.Configuration.GetSection("Logging"));

    var host = builder.Build();
    host.Run();
}
else if(OperatingSystem.IsLinux())
{
    // see: https://blog.maartenballiauw.be/post/2021/05/25/running-a-net-application-as-a-service-on-linux-with-systemd.html
    // see: https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux
    // CreateApplicationBuilder would be the 'modern' way of doing things, but the UseSystemd() extension only works on IHostBuilder :(
    var host = Host.CreateDefaultBuilder(args)  
    .UseSystemd()
    .ConfigureServices(services =>
    {
        services.AddSingleton<Worker>();
        services.AddHostedService<Worker>();
    })
    .Build();
    host.Run();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSingleton<Worker>();
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
