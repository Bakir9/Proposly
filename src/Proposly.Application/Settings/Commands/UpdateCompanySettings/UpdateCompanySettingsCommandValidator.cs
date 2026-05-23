using FluentValidation;

namespace Proposly.Application.Settings.Commands.UpdateCompanySettings;

public sealed class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.FiscalYearStartMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("Fiscal year start month must be between 1 (January) and 12 (December).");
    }
}
