using ImportaYa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportaYa.Infrastructure.Persistence.Configurations;

public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();

        builder.Property(j => j.FileName).HasMaxLength(260).IsRequired();
        builder.Property(j => j.StoredFilePath).HasMaxLength(260).IsRequired();

        builder.Property(j => j.Format).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(j => j.CreatedAt);
    }
}
