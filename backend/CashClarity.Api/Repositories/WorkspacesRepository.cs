using CashClarity.Api.Contracts;
using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IWorkspacesRepository
{
    Task<List<WorkspaceResponse>> GetWorkspaces(string userId);
    Task<WorkspaceResponse> CreateWorkspace(string? name, string userId);
    Task<bool> UserOwnsWorkspace(Guid workspaceId, string userId);
}

public class WorkspacesRepository(FinanceDbContext db) : IWorkspacesRepository
{
    public async Task<List<WorkspaceResponse>> GetWorkspaces(string userId)
    {
        var workspaces = await db.Workspaces
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.CreatedAt).ThenBy(w => w.Name)
            .ToListAsync();
        return workspaces.Select(Map).ToList();
    }

    public async Task<WorkspaceResponse> CreateWorkspace(string? name, string userId)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = WorkspaceNamePolicy.Normalize(name),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Workspaces.Add(workspace);
        foreach (var (code, accountName, type) in SystemAccountCatalog.All)
        {
            db.Accounts.Add(new Account
            {
                Code = code,
                Name = accountName,
                Type = type,
                IsSystem = true,
                UserId = userId,
                WorkspaceId = workspace.Id,
            });
        }
        await db.SaveChangesAsync();
        return Map(workspace);
    }

    public Task<bool> UserOwnsWorkspace(Guid workspaceId, string userId) =>
        db.Workspaces.AnyAsync(w => w.Id == workspaceId && w.UserId == userId);

    private static WorkspaceResponse Map(Workspace w) => new(w.Id.ToString(), w.Name, w.CreatedAt);
}
