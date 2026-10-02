namespace CashClarity.Api.Domain;

/// <summary>
/// Thrown when a requested resource does not exist or is not owned by the caller.
/// Mapped to HTTP 404 by <see cref="CashClarity.Api.ApiExceptionHandler"/>.
/// </summary>
public class NotFoundException(string message) : Exception(message);
