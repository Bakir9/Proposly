namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// The shape of an employment relationship.
/// <para>
/// Recorded for the contract and shown on reports. It suggests a starting figure for weekly hours
/// in the UI, but never constrains it — a full-time week is 38.5 hours in one company and 40 in
/// another, and part-time is whatever was agreed.
/// </para>
/// </summary>
public enum EmploymentType
{
    FullTime,
    PartTime,

    /// <summary>Geringfügige Beschäftigung / Minijob — employment below the statutory threshold.</summary>
    MarginalEmployment,

    /// <summary>Lehrling / Auszubildende.</summary>
    Apprentice,

    Other
}
