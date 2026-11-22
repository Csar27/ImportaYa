using ImportaYa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportaYa.Infrastructure.Persistence.Configurations;

public sealed class ImportLogEntryConfiguration : IEntityTypeConfiguration<ImportLogEntry>
{
    public void Configure(EntityTypeBuilder<ImportLogEntry> builder)
    {
        builder.ToTable("ImportLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.TipoDocumento).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Documento).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.Motivo).HasMaxLength(500).IsRequired();

        builder.HasOne<ImportJob>()
            .WithMany()
            .HasForeignKey(l => l.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.JobId, l.Tipo, l.Fila });
    }
}
