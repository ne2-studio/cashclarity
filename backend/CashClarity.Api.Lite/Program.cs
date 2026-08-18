using CashClarity.Api.Controllers;
using CashClarity.Api.Domain;
using CashClarity.Api.Lite;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authentication;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .WithHeaders("Content-Type", "Authorization", "x-client-info", "apikey", "x-test-user-id")
        .WithMethods("GET", "POST", "PATCH", "DELETE", "OPTIONS")
        .WithExposedHeaders("Content-Length")));

builder.Services.AddSingleton<List<JournalEntryResponse>>();
builder.Services.AddSingleton<List<BankMovementResponse>>();
builder.Services.AddSingleton<IAccountsRepository>(services =>
    new InMemoryAccountsRepository(
        services.GetRequiredService<List<JournalEntryResponse>>(),
        services.GetRequiredService<List<BankMovementResponse>>()));
builder.Services.AddSingleton<IJournalEntriesRepository>(services =>
    new InMemoryJournalEntriesRepository(
        services.GetRequiredService<List<JournalEntryResponse>>(),
        services.GetRequiredService<List<BankMovementResponse>>()));
builder.Services.AddSingleton<IBankMovementsRepository>(services =>
    new InMemoryBankMovementsRepository(services.GetRequiredService<List<BankMovementResponse>>()));
builder.Services.AddControllers().AddApplicationPart(typeof(AccountsController).Assembly);
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddAuthentication("FakeBearer")
    .AddScheme<AuthenticationSchemeOptions, FakeBearerAuthenticationHandler>("FakeBearer", _ => { });
builder.Services.AddAuthorization();

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
