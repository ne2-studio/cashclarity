using CashClarity.Api.Data;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .WithHeaders("Content-Type", "Authorization", "x-client-info", "apikey")
        .WithMethods("GET", "POST", "PATCH", "DELETE", "OPTIONS")
        .WithExposedHeaders("Content-Length")));

var databaseUrl = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FinanceDbContext>(opts => opts.UseNpgsql(databaseUrl));
builder.Services.AddScoped<FinanceRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "CashClarity API", 
        Version = "v1"
    });
});

// Add logging
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Add Authentication Services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.Audience = builder.Configuration["Auth:Audience"];
});

var app = builder.Build();

// Run database migrations
try
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<FinanceDbContext>();

    dbContext.Database.Migrate();

    app.Logger.LogInformation("Database migrations applied successfully");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Error applying database migrations");
    throw;
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CashClarity API v1");
    });
}

app.UseCors();

app.UseAuthorization();
app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
