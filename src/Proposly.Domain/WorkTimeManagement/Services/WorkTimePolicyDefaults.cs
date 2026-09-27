using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.WorkTimeManagement.Services;

/// <summary>
/// Starting values for a company's working time policy, by jurisdiction.
/// <para>
/// These are a convenience, not legal advice. Collective, sector, and company agreements are
/// frequently stricter, and legislation changes — which is why every value stays editable and why
/// the module flags rather than certifies. A jurisdiction with no defaults returns null so the
/// caller prompts an owner instead of guessing.
/// </para>
/// </summary>
public static class WorkTimePolicyDefaults
{
    public static IReadOnlyList<string> SupportedJurisdictions => ["AT", "DE"];

    /// <summary>
    /// Builds an unsaved policy seeded for <paramref name="jurisdiction"/>, or null if unsupported.
    /// </summary>
    public static WorkTimePolicy? For(
        string jurisdiction,
        Guid companyId,
        DateOnly validFrom,
        string? holidayRegionCode = null)
        => jurisdiction?.ToUpperInvariant() switch
        {
            "AT" => Austria(companyId, validFrom, holidayRegionCode),
            "DE" => Germany(companyId, validFrom, holidayRegionCode),
            _ => null
        };

    private static WorkTimePolicy Austria(Guid companyId, DateOnly validFrom, string? region)
    {
        var policy = WorkTimePolicy.Create(
            companyId, validFrom, "AT", region,
            maxHoursPerDay: 12m,
            maxHoursPerWeek: 60m,
            averagingWindowWeeks: 17,
            maxAverageHoursPerWeek: 48m,
            minDailyRestHours: 11m,
            minWeeklyRestHours: 36m,
            surplusCapHours: 80m,
            deficitFloorHours: -20m);

        policy.AddBreakRule(aboveHours: 6m, minBreakMinutes: 30);
        return policy;
    }

    private static WorkTimePolicy Germany(Guid companyId, DateOnly validFrom, string? region)
    {
        var policy = WorkTimePolicy.Create(
            companyId, validFrom, "DE", region,
            maxHoursPerDay: 10m,
            maxHoursPerWeek: 60m,
            averagingWindowWeeks: 24,
            maxAverageHoursPerWeek: 48m,
            minDailyRestHours: 11m,
            minWeeklyRestHours: 35m,
            surplusCapHours: 80m,
            deficitFloorHours: -20m);

        policy.AddBreakRule(aboveHours: 6m, minBreakMinutes: 30);
        policy.AddBreakRule(aboveHours: 9m, minBreakMinutes: 45);
        return policy;
    }
}
