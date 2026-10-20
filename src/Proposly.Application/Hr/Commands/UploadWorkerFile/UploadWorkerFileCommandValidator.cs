using FluentValidation;
using Proposly.Domain.HrManagement.Entities;

namespace Proposly.Application.Hr.Commands.UploadWorkerFile;

public sealed class UploadWorkerFileCommandValidator : AbstractValidator<UploadWorkerFileCommand>
{
    public const long MaxSizeBytes = 25 * 1024 * 1024;

    public UploadWorkerFileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(c => c.LongLength <= MaxSizeBytes)
            .WithMessage("The file is too large — the limit is 25 MB.");

        RuleFor(x => x.Category)
            .Must(c => c != null && EmployeeDocument.Categories.Contains(c))
            .When(x => x.Kind == WorkerFileKind.Document)
            .WithMessage("A document needs a valid category.");

        RuleFor(x => x.ContentType)
            .Must(ct => ct.StartsWith("image/"))
            .When(x => x.Kind == WorkerFileKind.Photo)
            .WithMessage("The photo must be an image.");
    }
}
