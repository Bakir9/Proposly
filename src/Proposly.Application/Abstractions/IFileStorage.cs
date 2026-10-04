namespace Proposly.Application.Abstractions;

/// <summary>
/// Blob storage behind an interface. Implementations decide where bytes live
/// (database today; S3/R2/Azure Blob would be a drop-in replacement later).
/// </summary>
public interface IFileStorage
{
    /// <summary>Stores the content and returns an opaque storage key.</summary>
    Task<string> SaveAsync(byte[] content, string contentType, CancellationToken ct = default);

    /// <summary>Returns the stored content, or null when the key does not exist (or belongs to another tenant).</summary>
    Task<byte[]?> OpenAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
