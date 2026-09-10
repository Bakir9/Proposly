using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.SubmitTimesheet;

public sealed record SubmitTimesheetCommand(int Year, int Month) : ICommand;
