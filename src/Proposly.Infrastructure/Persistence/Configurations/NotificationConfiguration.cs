using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.Notifications;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(500);
        builder.Property(n => n.Link).HasMaxLength(500);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
