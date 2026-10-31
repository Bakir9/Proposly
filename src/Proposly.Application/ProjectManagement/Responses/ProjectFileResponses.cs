namespace Proposly.Application.ProjectManagement.Responses;

/// <summary>
/// One row on the Collaborate › Files page. Source says where the file lives:
/// "Upload" = direct project upload (download via /projects/{id}/files/{fileId}),
/// "Discussion" = chat attachment in the project channel (download via /chat/attachments/{id}).
/// </summary>
public sealed record ProjectFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Source,
    string UploadedByName,
    DateTime UploadedAt,
    /// <summary>True when the caller may delete it (own upload, or admin).</summary>
    bool CanDelete);

/// <summary>Bytes for the authorized project-file download endpoint.</summary>
public sealed record ProjectFileContentResponse(
    string FileName,
    string ContentType,
    byte[] Content);
