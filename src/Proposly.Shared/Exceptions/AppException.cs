namespace Proposly.Shared.Exceptions;

/// <summary>
/// An expected application error carrying a stable machine-readable code and the HTTP status it
/// should produce. The global exception handler in the API serializes it as
/// <c>{ status, code, title }</c>, so the frontend can show the real reason and branch on the code.
/// Derives from <see cref="InvalidOperationException"/> so existing catch blocks and tests built
/// on the old "throw InvalidOperationException" convention keep working while call sites are
/// converted incrementally.
/// </summary>
public class AppException : InvalidOperationException
{
    /// <summary>Stable machine-readable identifier, e.g. "project.not_found".</summary>
    public string Code { get; }

    /// <summary>HTTP status code the global exception handler should respond with.</summary>
    public int StatusCode { get; }

    public AppException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public static AppException NotFound(string code, string message) => new(code, message, 404);

    public static AppException Forbidden(string code, string message) => new(code, message, 403);

    public static AppException Conflict(string code, string message) => new(code, message, 409);
}
