using FluentValidation;

namespace Proposly.Application.Hr.Commands.AddWorker;

public sealed class AddWorkerCommandValidator : AbstractValidator<AddWorkerCommand>
{
    public AddWorkerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JobTitle).MaximumLength(150);
    }
}
