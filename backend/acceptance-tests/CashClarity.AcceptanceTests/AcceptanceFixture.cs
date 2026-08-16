using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace CashClarity.AcceptanceTests;

public sealed class AcceptanceFixture : IAsyncLifetime
{
    private INetwork? network;
    private IContainer? postgres;
    private IContainer? api;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var image = Environment.GetEnvironmentVariable("BACKEND_IMAGE")
            ?? throw new InvalidOperationException("BACKEND_IMAGE is required");

        network = new NetworkBuilder().Build();
        await network.CreateAsync();

        postgres = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithNetwork(network)
            .WithNetworkAliases("postgres")
            .WithEnvironment("POSTGRES_USER", "test")
            .WithEnvironment("POSTGRES_PASSWORD", "test")
            .WithEnvironment("POSTGRES_DB", "cashclarity")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                "pg_isready", "-U", "test", "-d", "cashclarity"))
            .Build();

        await postgres.StartAsync();

        api = new ContainerBuilder()
            .WithImage(image)
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithEnvironment("ConnectionStrings__DefaultConnection", "Host=postgres;Port=5432;Database=cashclarity;Username=test;Password=test")
            .WithEnvironment("Auth__Authority", "http://fake-oidc.invalid")
            .WithEnvironment("Auth__Audience", "cashclarity-tests")
            .WithEnvironment("Auth__RequireHttpsMetadata", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                .ForPort(8080)
                .ForPath("/server/health")))
            .Build();

        await api.StartAsync();
        var port = api.GetMappedPublicPort(8080);
        Client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (api is not null) await api.DisposeAsync();
        if (postgres is not null) await postgres.DisposeAsync();
        if (network is not null) await network.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class AcceptanceCollection : ICollectionFixture<AcceptanceFixture>
{
    public const string Name = "acceptance";
}
