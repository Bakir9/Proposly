namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// How a record came to exist. Holiday import is deferred, so everything is Manual for now —
/// the value is stored from the start so imported entries stay distinguishable later.
/// </summary>
public enum EntrySource
{
    Manual,
    Imported
}
