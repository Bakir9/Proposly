using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddMilestone;

public sealed class AddMilestoneCommandValidator : AbstractValidator<AddMilestoneCommand>
{
    public AddMilestoneCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}
