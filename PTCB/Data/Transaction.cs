namespace PTCB.Data;

public class Transaction
{
    public int Id { get; set; }


    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }         
    public string Type { get; set; } = "Deposit"; // Deposit / Withdrawal / Transfer
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}