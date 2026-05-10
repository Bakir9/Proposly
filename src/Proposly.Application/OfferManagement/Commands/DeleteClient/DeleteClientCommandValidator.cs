using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.DeleteClient;

public sealed class DeleteClientCommandValidator : AbstractValidator<DeleteClientCommand>
{
    public DeleteClientCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
    }
}
