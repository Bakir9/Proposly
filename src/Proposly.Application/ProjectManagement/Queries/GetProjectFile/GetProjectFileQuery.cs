using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectFile;

/// <summary>Bytes of one direct project upload (chat attachments download via /api/chat).</summary>
public sealed record GetProjectFileQuery(Guid ProjectId, Guid FileId) : IQuery<ProjectFileContentResponse?>;
