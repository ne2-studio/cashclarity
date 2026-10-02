using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CashClarity.Api.Tests;

public class ApiExceptionHandlerTests
{
    [Fact]
    public void Maps_not_found_to_404()
        => Assert.Equal(404, ApiExceptionHandler.Map(new NotFoundException("x")).Status);

    [Fact]
    public void Maps_domain_rule_violations_to_400()
        => Assert.Equal(400, ApiExceptionHandler.Map(new AccountInUseException("a")).Status);

    [Fact]
    public void Maps_unexpected_errors_to_500_without_leaking_message()
    {
        var (status, message) = ApiExceptionHandler.Map(new Exception("secret connection string"));
        Assert.Equal(500, status);
        Assert.DoesNotContain("secret", message);
    }

    [Fact]
    public async Task Handler_writes_status_and_error_body()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        var handled = await new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance)
            .TryHandleAsync(ctx, new NotFoundException("nope"), default);

        Assert.True(handled);
        Assert.Equal(404, ctx.Response.StatusCode);
        ctx.Response.Body.Position = 0;
        Assert.Contains("nope", await new StreamReader(ctx.Response.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task Repositories_throw_NotFoundException_for_missing_resources()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new InMemoryAccountsRepository().UpdateAccount("missing", new Controllers.AccountPatchRequest(Name: "n"), "u"));
    }
}
