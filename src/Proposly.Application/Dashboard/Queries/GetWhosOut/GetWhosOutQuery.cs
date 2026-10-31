using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Responses;

namespace Proposly.Application.Dashboard.Queries.GetWhosOut;

/// <summary>Range: today | week | month (next 30 days).</summary>
public sealed record GetWhosOutQuery(string Range) : IQuery<WhosOutResponse>;
