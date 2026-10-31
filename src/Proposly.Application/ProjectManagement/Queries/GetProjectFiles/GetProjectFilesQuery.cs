using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectFiles;

/// <summary>All files of a project: direct uploads + the project channel's chat attachments.</summary>
public sealed record GetProjectFilesQuery(Guid ProjectId) : IQuery<IReadOnlyList<ProjectFileResponse>?>;
