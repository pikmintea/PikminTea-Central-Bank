using Microsoft.EntityFrameworkCore;

namespace PTCB.Data;


public class BankService
{
    private readonly ApplicationDbContext _usersDb;
    private readonly TransactionDbContext _txDb;

    public BankService(ApplicationDbContext usersDb, TransactionDbContext txDb)
    {
        _usersDb = usersDb;
        _txDb = txDb;
    }

    public async Task<decimal> GetBalanceAsync(string userId)
    {
        var user = await _usersDb.Users.FindAsync(userId);
        return user?.SumSumBalance ?? 0m;
    }

    public async Task<decimal> DepositAsync(string userId, decimal amount, string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        var user = await _usersDb.Users.FindAsync(userId)
                   ?? throw new InvalidOperationException("User not found.");

        user.SumSumBalance += amount;

        var tx = new Transaction
        {
            UserId = userId,
            Amount = amount,
            Type = "Deposit",
            Description = description,
            Timestamp = DateTime.UtcNow
        };
        _txDb.Transactions.Add(tx);

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return user.SumSumBalance;
    }

    public async Task<decimal> WithdrawAsync(string userId, decimal amount, string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        var user = await _usersDb.Users.FindAsync(userId)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.SumSumBalance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        user.SumSumBalance -= amount;

        var tx = new Transaction
        {
            UserId = userId,
            Amount = -amount,
            Type = "Withdrawal",
            Description = description,
            Timestamp = DateTime.UtcNow
        };
        _txDb.Transactions.Add(tx);

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return user.SumSumBalance;
    }

    public async Task<bool> TransferAsync(string fromUserId, string toUserId, decimal amount, string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        if (string.Equals(fromUserId, toUserId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot transfer to yourself.");

        var fromUser = await _usersDb.Users.FindAsync(fromUserId)
                       ?? throw new InvalidOperationException("Sender not found.");
        var toUser = await _usersDb.Users.FindAsync(toUserId)
                     ?? throw new InvalidOperationException("Recipient not found.");

        if (fromUser.SumSumBalance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        fromUser.SumSumBalance -= amount;
        toUser.SumSumBalance += amount;

        _txDb.Transactions.Add(new Transaction
        {
            UserId = fromUserId,
            Amount = -amount,
            Type = "Transfer",
            Description = description ?? $"Transfer to {toUserId}",
            Timestamp = DateTime.UtcNow
        });
        _txDb.Transactions.Add(new Transaction
        {
            UserId = toUserId,
            Amount = amount,
            Type = "Transfer",
            Description = description ?? $"Transfer from {fromUserId}",
            Timestamp = DateTime.UtcNow
        });

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return true;
    }

    public async Task<List<Transaction>> GetHistoryAsync(string userId)
    {
        return await _txDb.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }
    public async Task<decimal> SetBalanceAsync(string userId, decimal newBalance)
    {
        if (newBalance < 0)
            throw new ArgumentException("Balance cannot be negative.", nameof(newBalance));

        var user = await _usersDb.Users.FindAsync(userId)
                   ?? throw new InvalidOperationException("User not found.");

        var difference = newBalance - user.SumSumBalance;
        user.SumSumBalance = newBalance;

        _txDb.Transactions.Add(new Transaction
        {
            UserId = userId,
            Amount = difference,
            Type = "AdminAdjustment",
            Description = "Balance set by admin",
            Timestamp = DateTime.UtcNow
        });

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return user.SumSumBalance;
    }

    public async Task<List<Transaction>> GetAllTransactionsAsync()
    {
        return await _txDb.Transactions
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }
}
