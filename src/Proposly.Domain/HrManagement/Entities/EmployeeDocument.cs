using Proposly.Shared.Primitives;

namespace Proposly.Domain.HrManagement.Entities;

/// <summary>
/// A file attached to a worker (contract, amendment, certificate, …). Child of
/// <see cref="EmployeeProfile"/> — reach it through the aggregate, never directly.
/// </summary>
public sealed class EmployeeDocument : Entity<Guid>
{
    public const string ContractCategory = "Contract";
    public const string PreviousContractCategory = "Previous contract";

    public static readonly string[] Categories =
    [
        ContractCategory, "Amendment", PreviousContractCategory, "ID / Work permit",
        "Certificate", "Payroll", "Invoice", "Other",
    ];

    private EmployeeDocument() { } // For EF Core

    private EmployeeDocument(Guid id, Guid employeeProfileId, string category, string fileName, string contentType, long sizeBytes, string storageKey)
        : base(id)
    {
        EmployeeProfileId = employeeProfileId;
        Category = category;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        CreatedAt = DateTime.UtcNow;
    }

    internal static EmployeeDocument Create(Guid employeeProfileId, string category, string fileName, string contentType, long sizeBytes, string storageKey)
    {
        if (!Categories.Contains(category))
            throw new InvalidOperationException($"Unknown document category '{category}'.");
        return new EmployeeDocument(Guid.NewGuid(), employeeProfileId, category, fileName, contentType, sizeBytes, storageKey);
    }

    public Guid EmployeeProfileId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }

    /// <summary>Opaque key into IFileStorage — never exposed to clients.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    internal void Recategorize(string category)
    {
        if (!Categories.Contains(category))
            throw new InvalidOperationException($"Unknown document category '{category}'.");
        Category = category;
    }
}
