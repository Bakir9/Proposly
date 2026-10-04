using FluentValidation;

namespace Proposly.Application.Chat.Commands.UploadChatAttachment;

public sealed class UploadChatAttachmentCommandValidator : AbstractValidator<UploadChatAttachmentCommand>
{
    public const long MaxSizeBytes = 25 * 1024 * 1024;

    /// <summary>Images, PDFs, Office documents, CSV, DWG and ZIP — mirrors the composer's file picker.</summary>
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".heic", ".gif",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".dwg", ".zip",
    };

    public UploadChatAttachmentCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name)))
            .WithMessage("This file type is not allowed. Allowed: images, PDF, Word, Excel, CSV, DWG, ZIP.");

        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(c => c.LongLength <= MaxSizeBytes)
            .WithMessage("The file is too large — the limit is 25 MB.");
    }
}
