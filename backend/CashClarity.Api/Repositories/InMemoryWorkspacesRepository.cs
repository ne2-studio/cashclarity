using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryWorkspacesRepository : IWorkspacesRepository
{
    private readonly object gate = new();
    private readonly List<(WorkspaceResponse Workspace, string UserId)> workspaces = [];

    public Task<List<WorkspaceResponse>> GetWorkspaces(string userId)
    {
        lock (gate)
        {
            return Task.FromResult(workspaces
                .Where(w => w.UserId == userId)
                .Select(w => w.Workspace)
                .OrderBy(w => w.CreatedAt)
                .ToList());
        }
    }

    public Task<WorkspaceResponse> CreateWorkspace(string? name, string userId)
    {
        var normalized = WorkspaceNamePolicy.Normalize(name);
        lock (gate)
        {
            var workspace = new WorkspaceResponse(Guid.NewGuid().ToString(), normalized, DateTime.UtcNow);
            workspaces.Add((workspace, userId));
            return Task.FromResult(workspace);
        }
    }

    public Task<bool> UserOwnsWorkspace(Guid workspaceId, string userId)
    {
        lock (gate)
        {
            return Task.FromResult(workspaces.Any(w => w.UserId == userId && w.Workspace.Id == workspaceId.ToString()));
        }
    }
}
