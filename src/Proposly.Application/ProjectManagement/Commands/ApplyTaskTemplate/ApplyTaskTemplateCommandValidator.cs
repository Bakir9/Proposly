using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.ApplyTaskTemplate;

public sealed class ApplyTaskTemplateCommandValidator : AbstractValidator<ApplyTaskTemplateCommand>
{
    public ApplyTaskTemplateCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}
