using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(30);

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("numeric(18,2)").IsRequired();
            money.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
    }
}
