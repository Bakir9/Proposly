using Proposly.Application.Abstractions;

namespace Proposly.Application.Settings.Commands.UpdateCompanySettings;

public record UpdateCompanySettingsCommand(
    int FiscalYearStartMonth,
    string? CompanyEmail = null,
    string? CompanyPhone = null,
    string? CompanyStreet = null,
    string? CompanyCity = null,
    string? CompanyPostalCode = null,
    string? CompanyCountry = null,
    bool IsVatRegistered = false,
    string? CompanyVatNumber = null,
    decimal DefaultVatRate = 0,
    bool IsVatExempt = false,
    string? VatExemptReason = null) : ICommand;
