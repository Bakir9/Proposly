using Proposly.Application.Abstractions;

namespace Proposly.Application.Hr.Commands.SetWorkerDocumentCategory;

public sealed record SetWorkerDocumentCategoryCommand(Guid UserId, Guid DocumentId, string Category) : ICommand;
