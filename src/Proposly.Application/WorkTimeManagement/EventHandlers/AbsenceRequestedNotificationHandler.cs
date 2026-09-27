using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.Notifications;
using Proposly.Domain.WorkTimeManagement.Events;

namespace Proposly.Application.WorkTimeManagement.EventHandlers;

public sealed class AbsenceRequestedNotificationHandler(
    INotificationRepository notifications,
    IUserRepository users) : IDomainEventHandler<AbsenceRequestedDomainEvent>
{
    public async Task HandleAsync(AbsenceRequestedDomainEvent e, CancellationToken ct = default)
    {
        var employee = await users.GetByIdAsync(e.UserId, ct);
        var employeeName = employee?.FullName ?? "An employee";

        var approvers = (await users.GetActiveAsync(ct))
            .Where(u => u.Role is UserRole.Owner or UserRole.Admin)
            .ToList();

        if (approvers.Count == 0) return;

        // The type is named because it is what an approver decides on. No health detail exists to
        // leak — sick leave carries its type and dates only.
        var title = $"{employeeName} requested {Humanise(e.Type)} " +
                    $"({e.StartDate:dd MMM} – {e.EndDate:dd MMM}, {e.ConsumedDays} day(s))";

        foreach (var approver in approvers)
        {
            await notifications.AddAsync(
                Notification.Create(e.CompanyId, approver.Id, title, "/absences/approvals"), ct);
        }

        await notifications.SaveChangesAsync(ct);
    }

    private static string Humanise(Domain.WorkTimeManagement.Enums.AbsenceType type)
        => type switch
        {
            Domain.WorkTimeManagement.Enums.AbsenceType.Vacation => "vacation",
            Domain.WorkTimeManagement.Enums.AbsenceType.SickLeave => "sick leave",
            Domain.WorkTimeManagement.Enums.AbsenceType.UnpaidLeave => "unpaid leave",
            Domain.WorkTimeManagement.Enums.AbsenceType.ParentalLeave => "parental leave",
            Domain.WorkTimeManagement.Enums.AbsenceType.SpecialLeave => "special leave",
            _ => "time off"
        };
}
