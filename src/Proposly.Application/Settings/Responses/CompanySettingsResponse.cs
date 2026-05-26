using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.Application.Settings.Responses;

public record CompanySettingsResponse(
    int FiscalYearStartMonth,
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
