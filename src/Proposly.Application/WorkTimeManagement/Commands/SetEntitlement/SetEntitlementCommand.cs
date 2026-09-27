using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.SetEntitlement;

/// <summary>
/// Sets or adjusts an employee's vacation position for a year. Carry-over is entered by hand
/// because it does not expire automatically — forfeiture is a policy decision, not a default.
/// </summary>
public sealed record SetEntitlementCommand(
    Guid UserId,
    int Year,
    decimal EntitledDays,
    decimal CarriedOverDays = 0m) : ICommand;
