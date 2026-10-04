using Proposly.Application.Abstractions;
using Proposly.Infrastructure.Persistence;

namespace Proposly.Infrastructure.Services.Storage;

/// <summary>
/// IFileStorage backed by PostgreSQL. Chosen over local disk because Render's filesystem is
/// ephemeral — files on disk vanish on every deploy. A blob provider (S3/R2/Azure) can replace
/// this behind the same interface later without touching callers.
/// </summary>
public sealed class DatabaseFileStorage(AppDbContext context, ICurrentUserService currentUser) : IFileStorage
{
    public async Task<string> SaveAsync(byte[] content, string contentType, CancellationToken ct = default)
    {
        var file = StoredFile.Create(currentUser.CompanyId, contentType, content);
        await context.StoredFiles.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);
        return file.Id.ToString();
    }

    public async Task<byte[]?> OpenAsync(string storageKey, CancellationToken ct = default)
    {
        if (!Guid.TryParse(storageKey, out var id))
            return null;

        var file = await context.StoredFiles.FindAsync([id], ct);
        // FindAsync bypasses query filters when the entity is already tracked, so re-check the tenant.
        return file is null || file.CompanyId != currentUser.CompanyId ? null : file.Content;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (!Guid.TryParse(storageKey, out var id))
            return;

        var file = await context.StoredFiles.FindAsync([id], ct);
        if (file is null || file.CompanyId != currentUser.CompanyId)
            return;

        context.StoredFiles.Remove(file);
        await context.SaveChangesAsync(ct);
    }
}
