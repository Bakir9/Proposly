using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.OfferManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Title).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(4000);
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.DiscountPercent).HasColumnType("numeric(5,2)");

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.CompanyId);
        builder.HasIndex(o => o.ClientId);
    }
}
