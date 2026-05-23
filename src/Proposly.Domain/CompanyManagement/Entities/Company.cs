using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.CompanyManagement.Entities;

public sealed class Company : AggregateRoot<Guid>, IAuditableEntity
{
    private Company() { } // For EF Core

    private Company(Guid id, string name) : base(id)
    {
        Name = name;
        Status = CompanyStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Company Create(string name) => new(Guid.NewGuid(), name);
    public static Company Create(Guid id, string name) => new(id, name);

    public string Name { get; private set; } = string.Empty;
    public CompanyStatus Status { get; private set; }
    public int FiscalYearStartMonth { get; private set; } = 1;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // VAT settings
    public string? CompanyCountry { get; private set; }
    public bool IsVatRegistered { get; private set; }
    public string? CompanyVatNumber { get; private set; }
    public decimal DefaultVatRate { get; private set; }
    public bool IsVatExempt { get; private set; }
    public string? VatExemptReason { get; private set; }

    public void UpdateSettings(int fiscalYearStartMonth)
    {
        if (fiscalYearStartMonth is < 1 or > 12)
            throw new InvalidOperationException("Fiscal year start month must be between 1 and 12.");
        FiscalYearStartMonth = fiscalYearStartMonth;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateVatSettings(
        string? companyCountry,
        bool isVatRegistered,
        string? vatNumber,
        decimal defaultVatRate,
        bool isVatExempt,
        string? vatExemptReason)
    {
        if (defaultVatRate < 0 || defaultVatRate > 100)
            throw new InvalidOperationException("VAT rate must be between 0 and 100.");
        CompanyCountry = companyCountry;
        IsVatRegistered = isVatRegistered;
        CompanyVatNumber = vatNumber;
        DefaultVatRate = defaultVatRate;
        IsVatExempt = isVatExempt;
        VatExemptReason = vatExemptReason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status == CompanyStatus.Suspended)
            throw new InvalidOperationException("Company is already suspended.");
        Status = CompanyStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Status = CompanyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}
