using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.SetBillingType;

/// <summary>Switches how the project earns money: FixedPrice, TimeAndMaterials or Internal.</summary>
public sealed record SetBillingTypeCommand(Guid ProjectId, string BillingType) : ICommand;
