using System.Net.Http.Headers;
using System.Text.Json;
using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using dotenv.net;
using Microsoft.EntityFrameworkCore;

DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true));

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") ?? "";
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? "";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .WithHeaders("Content-Type", "Authorization", "x-client-info", "apikey")
        .WithMethods("GET", "POST", "PATCH", "DELETE", "OPTIONS")
        .WithExposedHeaders("Content-Length")));

builder.Services.AddHttpClient("supabase-auth", client =>
{
    client.BaseAddress = new Uri(supabaseUrl);
    client.DefaultRequestHeaders.Add("apikey", supabaseKey);
});

builder.Services.AddDbContext<FinanceDbContext>(opts => opts.UseNpgsql(databaseUrl));
builder.Services.AddScoped<FinanceRepository>();

var app = builder.Build();

app.UseCors();

// Verify Supabase JWT and extract userId into HttpContext.Items
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/server/health"))
    {
        await next(ctx);
        return;
    }

    var authHeader = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(authHeader))
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Missing Authorization header" });
        return;
    }

    var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authHeader["Bearer ".Length..]
        : authHeader;

    var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
    var authClient = factory.CreateClient("supabase-auth");

    using var authRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/v1/user");
    authRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    try
    {
        var authResponse = await authClient.SendAsync(authRequest);
        if (!authResponse.IsSuccessStatusCode)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = "Invalid token" });
            return;
        }

        var userDoc = await authResponse.Content.ReadFromJsonAsync<JsonElement>();
        ctx.Items["userId"] = userDoc.GetProperty("id").GetString()!;
    }
    catch
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Invalid token" });
        return;
    }

    await next(ctx);
});

static string UserId(HttpContext ctx) => (string)ctx.Items["userId"]!;

var server = app.MapGroup("/server");

server.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Accounts

server.MapGet("/accounts", async (HttpContext ctx, FinanceRepository repo) =>
{
    try { return Results.Ok(await repo.GetAccounts(UserId(ctx))); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapPost("/accounts", async (HttpContext ctx, AccountCreateRequest body, FinanceRepository repo) =>
{
    try { return Results.Ok(await repo.AddAccount(body, UserId(ctx))); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapPatch("/accounts/{id}", async (HttpContext ctx, string id, AccountPatchRequest body, FinanceRepository repo) =>
{
    try { await repo.UpdateAccount(id, body, UserId(ctx)); return Results.Ok(new { success = true }); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapDelete("/accounts/{id}", async (HttpContext ctx, string id, FinanceRepository repo) =>
{
    try { await repo.DeleteAccount(id, UserId(ctx)); return Results.Ok(new { success = true }); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

// Journal Entries

server.MapGet("/journal-entries", async (HttpContext ctx, FinanceRepository repo) =>
{
    try { return Results.Ok(await repo.GetJournalEntries(UserId(ctx))); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapPost("/journal-entries", async (HttpContext ctx, JournalEntryCreateRequest body, FinanceRepository repo) =>
{
    try { return Results.Ok(await repo.AddJournalEntry(body, UserId(ctx))); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapPatch("/journal-entries/{id}", async (HttpContext ctx, string id, JournalEntryPatchRequest body, FinanceRepository repo) =>
{
    try { await repo.UpdateJournalEntry(id, body, UserId(ctx)); return Results.Ok(new { success = true }); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapDelete("/journal-entries/{id}", async (HttpContext ctx, string id, FinanceRepository repo) =>
{
    try { await repo.DeleteJournalEntry(id, UserId(ctx)); return Results.Ok(new { success = true }); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

// Bank Movements

server.MapGet("/bank-movements", async (HttpContext ctx, FinanceRepository repo) =>
{
    try { return Results.Ok(await repo.GetBankMovements(UserId(ctx))); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapPost("/bank-movements", async (HttpContext ctx, BankMovementCreateRequest body, FinanceRepository repo) =>
{
    try { return Results.Ok(await repo.AddBankMovement(body, UserId(ctx))); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapPatch("/bank-movements/{id}", async (HttpContext ctx, string id, BankMovementPatchRequest body, FinanceRepository repo) =>
{
    try { await repo.UpdateBankMovement(id, body, UserId(ctx)); return Results.Ok(new { success = true }); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

server.MapDelete("/bank-movements/{id}", async (HttpContext ctx, string id, FinanceRepository repo) =>
{
    try { await repo.DeleteBankMovement(id, UserId(ctx)); return Results.Ok(new { success = true }); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message }, statusCode: 500); }
});

app.Run();
