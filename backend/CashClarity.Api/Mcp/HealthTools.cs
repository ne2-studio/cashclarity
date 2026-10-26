using System.ComponentModel;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

[McpServerToolType]
public static class HealthTools
{
    [McpServerTool(Name = "health_check", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Comprueba que la API de CashClarity responde. Equivale a GET /server/health. Devuelve { status: \"ok\" }.")]
    public static object HealthCheck() => new { status = "ok" };
}
