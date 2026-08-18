using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace CashClarity.AcceptanceTests;

public sealed class AcceptanceFixture : IAsyncLifetime
{
    public const string OidcClientId = "cashclarity-tests";
    public const string OidcUserKey = "primary";
    public const string OidcUserSub = "acceptance-user";
    public const string OidcSecondUserKey = "second";
    public const string OidcSecondUserSub = "second-acceptance-user";
    public const string OidcRedirectUri = "https://acceptance-test.cashclarity.invalid/callback";

    private INetwork? network;
    private IContainer? postgres;
    private IContainer? fakeOidc;
    private IContainer? api;

    public HttpClient Client { get; private set; } = null!;
    public HttpClient FakeOidcClient { get; private set; } = null!;
    public string DockerHost { get; } = Environment.GetEnvironmentVariable("TESTCONTAINERS_HOST_OVERRIDE") ?? "localhost";

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

        fakeOidc = new ContainerBuilder()
            .WithImage("ghcr.io/ne2-studio/fake-oidc:latest")
            .WithNetwork(network)
            .WithNetworkAliases("fake-oidc")
            .WithPortBinding(5000, true)
            .WithEnvironment("OIDC_ISSUER", "http://fake-oidc:5000")
            .WithEnvironment("OIDC_CLIENTS", $$"""[{"clientId":"{{OidcClientId}}","redirectUris":["{{OidcRedirectUri}}"]}]""")
            .WithEnvironment("OIDC_USERS", $$"""[{"key":"{{OidcUserKey}}","sub":"{{OidcUserSub}}","email":"primary@acceptance-test.cashclarity.invalid","name":"Primary Acceptance User","roles":[]},{"key":"{{OidcSecondUserKey}}","sub":"{{OidcSecondUserSub}}","email":"second@acceptance-test.cashclarity.invalid","name":"Second Acceptance User","roles":[]}]""")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                .ForPort(5000)
                .ForPath("/health")))
            .Build();

        await Task.WhenAll(postgres.StartAsync(), fakeOidc.StartAsync());

        api = new ContainerBuilder()
            .WithImage(image)
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithEnvironment("ConnectionStrings__DefaultConnection", "Host=postgres;Port=5432;Database=cashclarity;Username=test;Password=test")
            .WithEnvironment("Auth__Authority", "http://fake-oidc:5000")
            .WithEnvironment("Auth__Audience", OidcClientId)
            .WithEnvironment("Auth__RequireHttpsMetadata", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                .ForPort(8080)
                .ForPath("/server/health")))
            .Build();

        await api.StartAsync();
        var port = api.GetMappedPublicPort(8080);
        Client = new HttpClient { BaseAddress = new Uri($"http://{DockerHost}:{port}") };
        FakeOidcClient = new HttpClient { BaseAddress = new Uri($"http://{DockerHost}:{fakeOidc.GetMappedPublicPort(5000)}") };
    }

    public FakeOidcTokenClient CreateOidcTokenClient() => new(FakeOidcClient.BaseAddress!);

    public async Task DisposeAsync()
    {
        Client.Dispose();
        FakeOidcClient.Dispose();
        if (api is not null) await api.DisposeAsync();
        if (fakeOidc is not null && postgres is not null)
        {
            await Task.WhenAll(fakeOidc.DisposeAsync().AsTask(), postgres.DisposeAsync().AsTask());
        }
        else
        {
            if (fakeOidc is not null) await fakeOidc.DisposeAsync();
            if (postgres is not null) await postgres.DisposeAsync();
        }
        if (network is not null) await network.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class AcceptanceCollection : ICollectionFixture<AcceptanceFixture>
{
    public const string Name = "acceptance";
}
