using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetTaskTemplates;

public sealed record GetTaskTemplatesQuery() : IQuery<IReadOnlyList<TaskTemplateResponse>>;
