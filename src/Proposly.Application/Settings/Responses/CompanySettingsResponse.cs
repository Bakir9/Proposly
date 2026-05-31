using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.Application.Settings.Responses;

public record CompanySettingsResponse(
    int FiscalYearStartMonth,
    string? CompanyEmail,
    string? CompanyPhone,
    string? CompanyStreet,
    string? CompanyCity,
    string? CompanyPostalCode,
    string? CompanyCountry,
    bool IsVatRegistered,
    string? CompanyVatNumber,
    decimal DefaultVatRate,
    bool IsVatExempt,
    string? VatExemptReason,
    PlanTier PlanTier,
    int? MaxUsers,
    int? MaxProjects,
    DateTime? PlanExpiresAt,
    int CurrentUserCount,
    int CurrentProjectCount);
