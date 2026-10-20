using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;

namespace Proposly.Application.Hr.Commands.AddWorkerNote;

public sealed record AddWorkerNoteCommand(Guid UserId, string Tag, string Text) : ICommand<WorkerNoteResponse>;
