using Proposly.Application.Abstractions;
using Proposly.Application.Settings.Responses;

namespace Proposly.Application.Settings.Queries.GetCompanySettings;

public record GetCompanySettingsQuery : IQuery<CompanySettingsResponse?>;
