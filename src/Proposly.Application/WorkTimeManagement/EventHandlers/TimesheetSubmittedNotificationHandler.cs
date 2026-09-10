using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.Notifications;
using Proposly.Domain.WorkTimeManagement.Events;

namespace Proposly.Application.WorkTimeManagement.EventHandlers;

/// <summary>
/// Notifies everyone who can approve. There is no per-employee approver relationship in the role
/// model, so every Owner and Admin in the company is told.
/// </summary>
public sealed class TimesheetSubmittedNotificationHandler(
    INotificationRepository notifications,
    IUserRepository users) : IDomainEventHandler<TimesheetSubmittedDomainEvent>
{
    public async Task HandleAsync(TimesheetSubmittedDomainEvent e, CancellationToken ct = default)
    {
        var employee = await users.GetByIdAsync(e.UserId, ct);
        var employeeName = employee?.FullName ?? "An employee";

        var approvers = (await users.GetActiveAsync(ct))
            .Where(u => u.Role is UserRole.Owner or UserRole.Admin)
            .ToList();

        if (approvers.Count == 0) return;

        foreach (var approver in approvers)
        {
            var notification = Notification.Create(
                e.CompanyId,
                approver.Id,
                $"{employeeName} submitted working time for {e.Year}-{e.Month:00}",
                $"/worktime/approvals?timesheet={e.TimesheetId}");

            await notifications.AddAsync(notification, ct);
        }

        await notifications.SaveChangesAsync(ct);
    }
}
