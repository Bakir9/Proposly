using FluentValidation.Results;

namespace Proposly.Application.Abstractions;

public sealed class CommandValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public CommandValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}
