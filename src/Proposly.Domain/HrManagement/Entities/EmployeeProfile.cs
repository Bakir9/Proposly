using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.HrManagement.Entities;

/// <summary>
/// The HR record of one worker — personal data, CV, documents, allowances and internal notes.
/// 1:1 with a <c>User</c> (which may be profile-only, i.e. without login access).
/// Contract and schedule data stays in WorkTimeManagement's versioned EmploymentTerms.
/// </summary>
public sealed class EmployeeProfile : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<EmployeeDocument> _documents = [];
    private readonly List<EmployeeAllowance> _allowances = [];
    private readonly List<EmployeeNote> _notes = [];

    private EmployeeProfile() { } // For EF Core

    private EmployeeProfile(Guid id, Guid companyId, Guid userId) : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static EmployeeProfile Create(Guid companyId, Guid userId, string? jobTitle = null)
    {
        var profile = new EmployeeProfile(Guid.NewGuid(), companyId, userId);
        profile.JobTitle = jobTitle;
        return profile;
    }

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }

    public string? JobTitle { get; private set; }
    public string? Phone { get; private set; }
    public string? Gender { get; private set; }
    public DateOnly? Birthday { get; private set; }

    /// <summary>Sozialversicherungsnummer — sensitive; exposed to Owner/Admin only.</summary>
    public string? Svnr { get; private set; }

    public string? Nationality { get; private set; }
    public string? Department { get; private set; }
    public string? Location { get; private set; }
    public string? Address { get; private set; }
    public string? EmergencyName { get; private set; }
    public string? EmergencyRelation { get; private set; }
    public string? EmergencyPhone { get; private set; }

    /// <summary>Employment start/end as displayed in HR — independent of terms versioning.</summary>
    public DateOnly? EmploymentStart { get; private set; }
    public DateOnly? EmploymentEnd { get; private set; }

    public string? PhotoStorageKey { get; private set; }

    /// <summary>CV details (experience, education, skills, languages) as a JSON document — display-only data.</summary>
    public string? CvJson { get; private set; }

    public string? CvFileName { get; private set; }
    public long? CvFileSize { get; private set; }
    public DateOnly? CvFileUploadedAt { get; private set; }
    public string? CvStorageKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<EmployeeDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<EmployeeAllowance> Allowances => _allowances.AsReadOnly();
    public IReadOnlyCollection<EmployeeNote> Notes => _notes.AsReadOnly();

    public void UpdatePersonal(
        string? jobTitle, string? phone, string? gender, DateOnly? birthday, string? svnr,
        string? nationality, string? department, string? location, string? address,
        string? emergencyName, string? emergencyRelation, string? emergencyPhone,
        DateOnly? employmentStart, DateOnly? employmentEnd)
    {
        JobTitle = jobTitle;
        Phone = phone;
        Gender = gender;
        Birthday = birthday;
        Svnr = svnr;
        Nationality = nationality;
        Department = department;
        Location = location;
        Address = address;
        EmergencyName = emergencyName;
        EmergencyRelation = emergencyRelation;
        EmergencyPhone = emergencyPhone;
        EmploymentStart = employmentStart;
        EmploymentEnd = employmentEnd;
        Touch();
    }

    public void UpdateCv(string? cvJson)
    {
        CvJson = cvJson;
        Touch();
    }

    public void SetPhoto(string? storageKey)
    {
        PhotoStorageKey = storageKey;
        Touch();
    }

    public void SetCvFile(string fileName, long sizeBytes, string storageKey)
    {
        CvFileName = fileName;
        CvFileSize = sizeBytes;
        CvFileUploadedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        CvStorageKey = storageKey;
        Touch();
    }

    public void RemoveCvFile()
    {
        CvFileName = null;
        CvFileSize = null;
        CvFileUploadedAt = null;
        CvStorageKey = null;
        Touch();
    }

    /// <summary>
    /// Adds a document. A new signed contract demotes the previous one to "Previous contract",
    /// so there is always at most one current contract (rule from the design handoff).
    /// </summary>
    public EmployeeDocument AddDocument(string category, string fileName, string contentType, long sizeBytes, string storageKey)
    {
        if (category == EmployeeDocument.ContractCategory)
        {
            foreach (var doc in _documents.Where(d => d.Category == EmployeeDocument.ContractCategory))
                doc.Recategorize(EmployeeDocument.PreviousContractCategory);
        }

        var document = EmployeeDocument.Create(Id, category, fileName, contentType, sizeBytes, storageKey);
        _documents.Add(document);
        Touch();
        return document;
    }

    public EmployeeDocument RemoveDocument(Guid documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new InvalidOperationException($"Document {documentId} not found.");
        _documents.Remove(document);
        Touch();
        return document;
    }

    public void RecategorizeDocument(Guid documentId, string category)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new InvalidOperationException($"Document {documentId} not found.");
        document.Recategorize(category);
        Touch();
    }

    /// <summary>Replaces the allowance list wholesale — the HR page edits it as one unit.</summary>
    public void ReplaceAllowances(IEnumerable<(string Name, Money Amount, string Unit)> allowances)
    {
        _allowances.Clear();
        foreach (var (name, amount, unit) in allowances)
            _allowances.Add(EmployeeAllowance.Create(Id, name, amount, unit));
        Touch();
    }

    public EmployeeNote AddNote(string tag, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("A note needs text.");
        var note = EmployeeNote.Create(Id, tag, text.Trim());
        _notes.Add(note);
        Touch();
        return note;
    }

    public void RemoveNote(Guid noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId)
            ?? throw new InvalidOperationException($"Note {noteId} not found.");
        _notes.Remove(note);
        Touch();
    }

    public void SetNotePinned(Guid noteId, bool pinned)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId)
            ?? throw new InvalidOperationException($"Note {noteId} not found.");
        note.SetPinned(pinned);
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
