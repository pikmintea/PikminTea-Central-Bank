using Microsoft.EntityFrameworkCore;

namespace PTCB.Data;


public class BankService
{
    public const string CentralBankUserName = "central-bank";

    private static readonly char[] CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ0123456789".ToCharArray();

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

    public async Task<ApplicationUser?> FindByUserNameAsync(string userName)
    {
        return await _usersDb.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<decimal> GiveFromCentralBankAsync(string toUserId, decimal amount, string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        var fromUser = await FindByUserNameAsync(CentralBankUserName)
                       ?? throw new InvalidOperationException("Central bank account not found.");
        var toUser = await _usersDb.Users.FindAsync(toUserId)
                     ?? throw new InvalidOperationException("Recipient not found.");

        if (fromUser.SumSumBalance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        fromUser.SumSumBalance -= amount;
        toUser.SumSumBalance += amount;

        var desc = description ?? $"Gift from {CentralBankUserName}";

        _txDb.Transactions.Add(new Transaction
        {
            UserId = fromUser.Id,
            Amount = -amount,
            Type = "Give",
            Description = desc,
            Timestamp = DateTime.UtcNow
        });
        _txDb.Transactions.Add(new Transaction
        {
            UserId = toUserId,
            Amount = amount,
            Type = "Give",
            Description = desc,
            Timestamp = DateTime.UtcNow
        });

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return toUser.SumSumBalance;
    }

    public async Task<string> CreateGiftCardAsync(string userId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        var creator = await _usersDb.Users.FindAsync(userId)
                      ?? throw new InvalidOperationException("User not found.");
        var centralBank = await FindByUserNameAsync(CentralBankUserName)
                          ?? throw new InvalidOperationException("Central bank account not found.");

        if (creator.SumSumBalance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        var code = await GenerateUniqueCodeAsync();

        creator.SumSumBalance -= amount;
        centralBank.SumSumBalance += amount;

        _txDb.GiftCards.Add(new GiftCard
        {
            Code = code,
            Amount = amount,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        _txDb.Transactions.Add(new Transaction
        {
            UserId = userId,
            Amount = -amount,
            Type = "GiftCardCreate",
            Description = $"Left gift card {code}",
            Timestamp = DateTime.UtcNow
        });
        _txDb.Transactions.Add(new Transaction
        {
            UserId = centralBank.Id,
            Amount = amount,
            Type = "GiftCardCreate",
            Description = $"Escrow for gift card {code}",
            Timestamp = DateTime.UtcNow
        });

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return code;
    }

    public async Task<decimal> RedeemGiftCardAsync(string userId, string code)
    {
        code = code.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code is required.", nameof(code));

        var gcard = await _txDb.GiftCards.FirstOrDefaultAsync(g => g.Code == code)
                    ?? throw new InvalidOperationException("Gift card not found.");
        if (gcard.IsUsed)
            throw new InvalidOperationException("Gift card has already been used.");

        var redeemer = await _usersDb.Users.FindAsync(userId)
                       ?? throw new InvalidOperationException("User not found.");
        var centralBank = await FindByUserNameAsync(CentralBankUserName)
                          ?? throw new InvalidOperationException("Central bank account not found.");

        if (centralBank.SumSumBalance < gcard.Amount)
            throw new InvalidOperationException("Central bank has insufficient funds.");

        centralBank.SumSumBalance -= gcard.Amount;
        redeemer.SumSumBalance += gcard.Amount;

        gcard.IsUsed = true;
        gcard.UsedByUserId = userId;
        gcard.UsedAt = DateTime.UtcNow;

        _txDb.Transactions.Add(new Transaction
        {
            UserId = centralBank.Id,
            Amount = -gcard.Amount,
            Type = "GiftCardRedeem",
            Description = $"Gift card {code} redeemed",
            Timestamp = DateTime.UtcNow
        });
        _txDb.Transactions.Add(new Transaction
        {
            UserId = userId,
            Amount = gcard.Amount,
            Type = "GiftCardRedeem",
            Description = $"Redeemed gift card {code}",
            Timestamp = DateTime.UtcNow
        });

        await _usersDb.SaveChangesAsync();
        await _txDb.SaveChangesAsync();

        return redeemer.SumSumBalance;
    }

    public async Task<List<GiftCard>> GetMyGiftCardsAsync(string userId)
    {
        return await _txDb.GiftCards
            .Where(g => g.CreatedByUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        var random = new Random();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var code = string.Join("-", Enumerable.Range(0, 3).Select(_ => RandomGroup(random)));
            if (await _txDb.GiftCards.AllAsync(g => g.Code != code))
                return code;
        }
        throw new InvalidOperationException("Could not generate a unique gift card code.");
    }

    private static string RandomGroup(Random random)
    {
        var chars = new char[3];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = CodeAlphabet[random.Next(CodeAlphabet.Length)];
        return new string(chars);
    }
}
