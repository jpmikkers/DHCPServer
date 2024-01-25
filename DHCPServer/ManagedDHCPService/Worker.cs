using GitHub.JPMikkers.DHCP;

namespace ManagedDHCPService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private DHCPServerConfigurationList _configuration = new();
    private List<DHCPServerResurrector> _servers = new();

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }

    private static string GetDHCPServerApplicationDataFolder()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JPMikkers\\DHCP Server");
    }

    private static string GetConfigurationPath()
    {
        return Path.Combine(GetDHCPServerApplicationDataFolder(), "Configuration.xml");
    }

    public static string GetClientInfoPath(string serverName, string serverAddress)
    {
        return Path.Combine(GetDHCPServerApplicationDataFolder(), $"{serverName}_{serverAddress.Replace('.', '_')}.xml");
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var configurationPath = GetConfigurationPath();

        _logger.LogInformation($"Reading DHCP configuration '{configurationPath}'");

        _configuration = DHCPServerConfigurationList.Read(GetConfigurationPath());
        _servers = new List<DHCPServerResurrector>();

        foreach(DHCPServerConfiguration config in _configuration)
        {
            _servers.Add(new DHCPServerResurrector(config, _logger, GetClientInfoPath(config.Name, config.Address)));
        }

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        foreach(DHCPServerResurrector server in _servers)
        {
            server.Dispose();
        }
        _servers.Clear();
        return base.StopAsync(cancellationToken);
    }
}
