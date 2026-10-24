using Microsoft.AspNetCore.Authentication;
using ModelContextProtocol;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;

namespace CashClarity.Api.Mcp;

public static class McpServerSetup
{
    public const string Path = "/mcp";

    /// <summary>
    /// Registers the MCP server whose tools mirror the REST controllers one to one.
    /// Tools call the same repositories and services as the controllers, so no business logic lives here.
    /// </summary>
    public static IServiceCollection AddCashClarityMcp(this IServiceCollection services)
    {
        services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "cashclarity", Version = "1.0.0" })
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools((IEnumerable<Type>)ToolTypes)
            .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                try
                {
                    return await next(context, cancellationToken);
                }
                catch (Exception exception) when (exception is not McpException and not OperationCanceledException)
                {
                    return ToToolError(context.Services, exception);
                }
            }));
        return services;
    }

    /// <summary>
    /// Publishes the OAuth protected resource metadata (RFC 9728) so MCP clients discover the same OIDC
    /// provider the frontend uses. Token validation stays with the JwtBearer scheme.
    /// </summary>
    public static AuthenticationBuilder AddCashClarityMcpChallenge(this AuthenticationBuilder builder, IConfiguration configuration) =>
        builder.AddMcp(options =>
        {
            var resourceUrl = configuration["Mcp:ResourceUrl"];
            options.ResourceMetadata = new ProtectedResourceMetadata
            {
                Resource = string.IsNullOrEmpty(resourceUrl) ? null : resourceUrl,
                AuthorizationServers = { configuration["Auth:Authority"]! },
                ScopesSupported = configuration.GetSection("Mcp:Scopes").Get<List<string>>() ?? ["openid", "profile", "offline_access"],
                ResourceName = "CashClarity",
            };
        });

    public static IEndpointConventionBuilder MapCashClarityMcp(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapMcp(Path).RequireAuthorization();

    public static readonly Type[] ToolTypes =
    [
        typeof(HealthTools),
        typeof(WorkspacesTools),
        typeof(AccountsTools),
        typeof(JournalEntriesTools),
        typeof(BankMovementsTools),
    ];

    /// <summary>
    /// Reuses <see cref="ApiExceptionHandler.Map"/> so a tool reports the same message the REST endpoint would.
    /// </summary>
    private static CallToolResult ToToolError(IServiceProvider? services, Exception exception)
    {
        var (status, message) = ApiExceptionHandler.Map(exception);
        var logger = services?.GetService<ILoggerFactory>()?.CreateLogger(typeof(McpServerSetup));
        if (status >= 500)
            logger?.LogError(exception, "Unhandled exception in MCP tool");
        else
            logger?.LogInformation("MCP tool failed with {Status}: {Message}", status, exception.Message);

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = $"{status}: {message}" }],
        };
    }
}
