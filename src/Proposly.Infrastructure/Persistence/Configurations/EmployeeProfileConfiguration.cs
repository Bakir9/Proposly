using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.HrManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("EmployeeProfiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.JobTitle).HasMaxLength(150);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Gender).HasMaxLength(40);
        builder.Property(p => p.Svnr).HasMaxLength(40);
        builder.Property(p => p.Nationality).HasMaxLength(100);
        builder.Property(p => p.Department).HasMaxLength(100);
        builder.Property(p => p.Location).HasMaxLength(150);
        builder.Property(p => p.Address).HasMaxLength(300);
        builder.Property(p => p.EmergencyName).HasMaxLength(150);
        builder.Property(p => p.EmergencyRelation).HasMaxLength(100);
        builder.Property(p => p.EmergencyPhone).HasMaxLength(50);
        builder.Property(p => p.PhotoStorageKey).HasMaxLength(100);
        builder.Property(p => p.CvJson);
        builder.Property(p => p.CvFileName).HasMaxLength(255);
        builder.Property(p => p.CvStorageKey).HasMaxLength(100);

        builder.HasMany(p => p.Documents)
            .WithOne()
            .HasForeignKey(d => d.EmployeeProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Allowances)
            .WithOne()
            .HasForeignKey(a => a.EmployeeProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Notes)
            .WithOne()
            .HasForeignKey(n => n.EmployeeProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Allowances).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => new { p.CompanyId, p.UserId }).IsUnique();
    }
}

public sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("EmployeeDocuments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Category).IsRequired().HasMaxLength(40);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(d => d.StorageKey).IsRequired().HasMaxLength(100);
    }
}

public sealed class EmployeeAllowanceConfiguration : IEntityTypeConfiguration<EmployeeAllowance>
{
    public void Configure(EntityTypeBuilder<EmployeeAllowance> builder)
    {
        builder.ToTable("EmployeeAllowances");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Unit).IsRequired().HasMaxLength(20);

        builder.OwnsOne(a => a.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
    }
}

public sealed class EmployeeNoteConfiguration : IEntityTypeConfiguration<EmployeeNote>
{
    public void Configure(EntityTypeBuilder<EmployeeNote> builder)
    {
        builder.ToTable("EmployeeNotes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Tag).IsRequired().HasMaxLength(30);
        builder.Property(n => n.Text).IsRequired().HasMaxLength(4000);
    }
}
