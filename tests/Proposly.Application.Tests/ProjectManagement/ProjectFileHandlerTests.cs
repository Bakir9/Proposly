using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.DeleteProjectFile;
using Proposly.Application.ProjectManagement.Commands.UploadProjectFile;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Tests.ProjectManagement;

public sealed class ProjectFileHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IProjectFileRepository _files = Substitute.For<IProjectFileRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Project _project;

    public ProjectFileHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(_userId);
        _project = Project.Create(_companyId, "P", null, Guid.NewGuid(), "Client",
            new Money(1000m, "EUR"), new DateOnly(2026, 1, 1), null);
        _projects.GetByIdForWriteAsync(_project.Id, Arg.Any<CancellationToken>()).Returns(_project);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(User.Create(_companyId, "u@test.com", "hash", "Uploading", "User"));
    }

    [Fact]
    public async Task Upload_stores_bytes_and_metadata_with_uploader_snapshot()
    {
        _storage.SaveAsync(Arg.Any<byte[]>(), "application/pdf", Arg.Any<CancellationToken>()).Returns("key-1");
        ProjectFile? saved = null;
        await _files.AddAsync(Arg.Do<ProjectFile>(f => saved = f), Arg.Any<CancellationToken>());
        var sut = new UploadProjectFileCommandHandler(_projects, _files, _users, _storage, _currentUser);

        var response = await sut.HandleAsync(new UploadProjectFileCommand(_project.Id, "plan.pdf", "application/pdf", [1, 2, 3]));

        Assert.NotNull(saved);
        Assert.Equal("key-1", saved!.StorageKey);
        Assert.Equal(_companyId, saved.CompanyId);
        Assert.Equal("Uploading User", saved.UploadedByName);
        Assert.Equal(3, saved.SizeBytes);
        Assert.Equal("Upload", response.Source);
        Assert.True(response.CanDelete);
    }

    [Fact]
    public async Task Upload_to_an_invisible_project_is_refused()
    {
        var sut = new UploadProjectFileCommandHandler(_projects, _files, _users, _storage, _currentUser);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            sut.HandleAsync(new UploadProjectFileCommand(Guid.NewGuid(), "plan.pdf", "application/pdf", [1])));
    }

    [Fact]
    public async Task Delete_by_a_stranger_without_admin_view_is_forbidden()
    {
        var file = ProjectFile.Create(_companyId, _project.Id, "plan.pdf", "application/pdf", 3, "key-1", Guid.NewGuid(), "Someone Else");
        _files.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);
        _currentUser.CanViewAllEmployees.Returns(false);
        var sut = new DeleteProjectFileCommandHandler(_files, _storage, _currentUser);

        var ex = await Assert.ThrowsAnyAsync<AppException>(() =>
            sut.HandleAsync(new DeleteProjectFileCommand(_project.Id, file.Id)));
        Assert.Equal(403, ex.StatusCode);
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_by_the_uploader_removes_blob_and_row()
    {
        var file = ProjectFile.Create(_companyId, _project.Id, "plan.pdf", "application/pdf", 3, "key-1", _userId, "Uploading User");
        _files.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);
        var sut = new DeleteProjectFileCommandHandler(_files, _storage, _currentUser);

        await sut.HandleAsync(new DeleteProjectFileCommand(_project.Id, file.Id));

        await _storage.Received(1).DeleteAsync("key-1", Arg.Any<CancellationToken>());
        await _files.Received(1).RemoveAsync(file, Arg.Any<CancellationToken>());
    }
}
