namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// How working time is organized, independent of the legal relationship (EmploymentType):
/// a fixed weekly schedule (full- or part-time) or purely tracked hours.
/// </summary>
public enum WorkModel
{
    FullTime,
    PartTime,
    Hourly,
}
