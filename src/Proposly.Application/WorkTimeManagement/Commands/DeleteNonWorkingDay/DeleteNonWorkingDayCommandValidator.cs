using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.DeleteNonWorkingDay;

public sealed class DeleteNonWorkingDayCommandValidator
    : AbstractValidator<DeleteNonWorkingDayCommand>
{
    public DeleteNonWorkingDayCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
