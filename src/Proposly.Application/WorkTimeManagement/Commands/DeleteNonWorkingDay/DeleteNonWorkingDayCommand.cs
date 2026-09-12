using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.DeleteNonWorkingDay;

public sealed record DeleteNonWorkingDayCommand(Guid Id) : ICommand;
