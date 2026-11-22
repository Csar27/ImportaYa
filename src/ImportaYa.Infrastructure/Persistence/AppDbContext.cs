using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImportaYa.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportLogEntry> ImportLogs => Set<ImportLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
