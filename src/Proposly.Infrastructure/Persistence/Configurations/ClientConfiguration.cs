using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.OfferManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ContactPerson).HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("Street").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("City").HasMaxLength(100);
            a.Property(x => x.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            a.Property(x => x.Country).HasColumnName("Country").HasMaxLength(100);
        });

        builder.HasIndex(c => c.CompanyId);
    }
}
