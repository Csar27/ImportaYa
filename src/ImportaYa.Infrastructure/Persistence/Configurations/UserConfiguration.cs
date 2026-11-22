using ImportaYa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportaYa.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.TipoDocumento)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(u => u.Documento).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Telefono).HasMaxLength(30);

        builder.HasIndex(u => new { u.TipoDocumento, u.Documento })
            .IsUnique()
            .HasDatabaseName("UX_Users_TipoDocumento_Documento");
    }
}
