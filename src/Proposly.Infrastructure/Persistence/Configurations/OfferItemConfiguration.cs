using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.OfferManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class OfferItemConfiguration : IEntityTypeConfiguration<OfferItem>
{
    public void Configure(EntityTypeBuilder<OfferItem> builder)
    {
        builder.ToTable("OfferItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,4)").IsRequired();

        builder.OwnsOne(i => i.UnitPrice, m =>
        {
            m.Property(x => x.Amount).HasColumnName("UnitPriceAmount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(i => i.LineTotal);

        builder.HasIndex(i => i.OfferId);
    }
}
