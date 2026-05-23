using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.OfferManagement.Enums;

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

        // VAT
        builder.Property(o => o.VatRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
        builder.Property(o => o.VatType).HasConversion<string>().HasMaxLength(30).HasDefaultValue(VatType.Exempt);
        builder.Property(o => o.VatLabel).HasMaxLength(100).HasDefaultValue("No VAT");
        builder.Property(o => o.VatNote).HasMaxLength(500);
        builder.Property(o => o.IsVatExempt).HasDefaultValue(true);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.CompanyId);
        builder.HasIndex(o => o.ClientId);
    }
}
