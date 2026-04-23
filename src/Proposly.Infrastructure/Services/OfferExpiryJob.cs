using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proposly.Domain.OfferManagement.Enums;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Infrastructure.Services;

public sealed class OfferExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OfferExpiryJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public OfferExpiryJob(IServiceScopeFactory scopeFactory, ILogger<OfferExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExpireOverdueOffersAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ExpireOverdueOffersAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var offerRepo = scope.ServiceProvider.GetRequiredService<IOfferRepository>();

            var sentOffers = await offerRepo.GetByStatusAsync(OfferStatus.Sent, ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var expired = sentOffers
                .Where(o => o.ValidUntil.HasValue && o.ValidUntil.Value < today)
                .ToList();

            foreach (var offer in expired)
            {
                offer.Expire();
                await offerRepo.UpdateAsync(offer, ct);
            }

            if (expired.Count > 0)
                _logger.LogInformation("Expired {Count} overdue offer(s).", expired.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error while expiring overdue offers.");
        }
    }
}
