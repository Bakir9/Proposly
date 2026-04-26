using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Proposly.Application.Abstractions;

internal sealed class ValidatingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    IServiceProvider sp) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var validator = sp.GetService<IValidator<TCommand>>();
        if (validator is not null)
        {
            var result = await validator.ValidateAsync(command, cancellationToken);
            if (!result.IsValid)
                throw new CommandValidationException(result.Errors);
        }
        await inner.HandleAsync(command, cancellationToken);
    }
}

internal sealed class ValidatingCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IServiceProvider sp) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var validator = sp.GetService<IValidator<TCommand>>();
        if (validator is not null)
        {
            var result = await validator.ValidateAsync(command, cancellationToken);
            if (!result.IsValid)
                throw new CommandValidationException(result.Errors);
        }
        return await inner.HandleAsync(command, cancellationToken);
    }
}
