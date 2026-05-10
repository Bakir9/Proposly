using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Commands.AddClientNote;

public sealed class AddClientNoteCommandHandler : ICommandHandler<AddClientNoteCommand, Guid>
{
    private readonly IClientRepository _clients;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public AddClientNoteCommandHandler(IClientRepository clients, IUserRepository users, ICurrentUserService currentUser)
    {
        _clients = clients;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(AddClientNoteCommand command, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(command.ClientId, ct)
            ?? throw new InvalidOperationException($"Client {command.ClientId} not found.");

        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Current user not found.");

        var note = client.AddNote(command.Content, _currentUser.UserId, $"{user.FirstName} {user.LastName}");
        await _clients.UpdateAsync(client, ct);
        return note.Id;
    }
}
