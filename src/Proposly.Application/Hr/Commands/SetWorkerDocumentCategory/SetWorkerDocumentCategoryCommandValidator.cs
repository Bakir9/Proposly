using FluentValidation;
using Proposly.Domain.HrManagement.Entities;

namespace Proposly.Application.Hr.Commands.SetWorkerDocumentCategory;

public sealed class SetWorkerDocumentCategoryCommandValidator : AbstractValidator<SetWorkerDocumentCategoryCommand>
{
    public SetWorkerDocumentCategoryCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Category)
            .Must(c => EmployeeDocument.Categories.Contains(c))
            .WithMessage("Unknown document category.");
    }
}
