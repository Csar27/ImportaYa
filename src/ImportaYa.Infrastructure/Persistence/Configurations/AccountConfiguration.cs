using ImportaYa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportaYa.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Email).HasMaxLength(256).IsRequired();
        builder.Property(a => a.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(a => a.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Accounts_NormalizedEmail");
    }
}
