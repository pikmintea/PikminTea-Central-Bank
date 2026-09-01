using Microsoft.EntityFrameworkCore;

namespace PTCB.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<GiftCard>().HasIndex(g => g.Code).IsUnique();
    }
}