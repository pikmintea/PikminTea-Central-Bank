using Microsoft.EntityFrameworkCore;

namespace PTCB.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    
}