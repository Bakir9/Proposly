using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.DeleteWorkDay;

public sealed record DeleteWorkDayCommand(int Year, int Month, DateOnly Date) : ICommand;
