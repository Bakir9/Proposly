namespace Proposly.Application.Hr.Responses;

public sealed record WorkerListItemResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string? JobTitle,
    string EmploymentType,
    string WorkModel,
    decimal WeeklyHours,
    string AccountStatus,
    bool HasPhoto);

public sealed record WorkerAllowanceResponse(string Name, decimal Amount, string Currency, string Unit);

public sealed record WorkerNoteResponse(Guid Id, string Tag, string Text, DateOnly Date, bool Pinned);

public sealed record WorkerDocumentResponse(
    Guid Id, string Category, string FileName, long SizeBytes, DateOnly UploadedAt);

public sealed record WorkerAbsenceResponse(
    Guid Id, string Type, DateOnly From, DateOnly To, decimal Days, string Status);

public sealed record WorkerResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string Role,
    string AccountStatus, // ProfileOnly | Invited | Active | Disabled
    // profile
    string? JobTitle,
    string? Phone,
    string? Gender,
    DateOnly? Birthday,
    string? Svnr,
    string? Nationality,
    string? Department,
    string? Location,
    string? Address,
    string? EmergencyName,
    string? EmergencyRelation,
    string? EmergencyPhone,
    DateOnly? EmploymentStart,
    DateOnly? EmploymentEnd,
    bool HasPhoto,
    // cv
    string? CvJson,
    string? CvFileName,
    long? CvFileSize,
    DateOnly? CvFileUploadedAt,
    // contract (from the latest EmploymentTerms version)
    string EmploymentType,
    string WorkModel,
    decimal WeeklyHours,
    IReadOnlyList<decimal?> DayHours,
    decimal? HourlyRateAmount,
    string? HourlyRateCurrency,
    decimal? MaxHoursPerMonth,
    decimal? ExpectedWeeklyHours,
    // rules
    string OvertimeMode,
    decimal? OvertimeSurchargePct,
    decimal? NightSurchargePct,
    decimal? SundaySurchargePct,
    decimal? HolidaySurchargePct,
    bool TrackingRequired,
    bool AutoDeductBreak,
    bool Flexitime,
    bool OnCallDuty,
    TimeOnly? CoreFrom,
    TimeOnly? CoreTo,
    decimal? HomeOfficeDaysPerWeek,
    decimal? MaxHoursPerDay,
    // vacation
    decimal AnnualVacationDays,
    decimal CarriedOverDays,
    IReadOnlyList<WorkerAbsenceResponse> Absences,
    // lists
    IReadOnlyList<WorkerAllowanceResponse> Allowances,
    IReadOnlyList<WorkerNoteResponse> Notes,
    IReadOnlyList<WorkerDocumentResponse> Documents);

public sealed record HrFileResponse(string FileName, string ContentType, byte[] Content);
