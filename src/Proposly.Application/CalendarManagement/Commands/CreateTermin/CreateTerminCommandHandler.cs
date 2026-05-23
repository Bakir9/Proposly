using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Entities;
using Proposly.Domain.CalendarManagement.Repositories;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Commands.CreateTermin;

public sealed class CreateTerminCommandHandler : ICommandHandler<CreateTerminCommand, Guid>
{
    private readonly ITerminRepository _terminRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateTerminCommandHandler(ITerminRepository terminRepository, IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _terminRepository = terminRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateTerminCommand command, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var organizer = users.FirstOrDefault(u => u.Id == _currentUser.UserId)
            ?? throw new InvalidOperationException("Current user not found.");

        var termin = Termin.Create(
            _currentUser.CompanyId,
            command.Title, command.Description,
            command.Start, command.End, command.Location,
            organizer.Id, $"{organizer.FirstName} {organizer.LastName}");

        var invitees = new List<(Guid UserId, string Name)>();
        foreach (var userId in command.InviteeUserIds.Distinct())
        {
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null) continue;
            var inv = termin.AddInvitee(user.Id, $"{user.FirstName} {user.LastName}");
            invitees.Add((user.Id, inv.InviteeName));
        }

        termin.PublishScheduled();
        await _terminRepository.AddAsync(termin, cancellationToken);
        return termin.Id;
    }
}
