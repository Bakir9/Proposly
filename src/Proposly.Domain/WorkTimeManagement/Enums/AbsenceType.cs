namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// Kinds of absence. Only <see cref="Vacation"/> consumes annual entitlement.
/// <para>
/// Sick leave is identified by this value and its dates alone. No diagnosis, medical note, or
/// other health detail is recorded anywhere on an absence — see AbsenceRequest.
/// </para>
/// </summary>
public enum AbsenceType
{
    Vacation,
    SickLeave,
    UnpaidLeave,
    ParentalLeave,
    SpecialLeave
}
