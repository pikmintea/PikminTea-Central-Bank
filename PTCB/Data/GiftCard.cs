namespace PTCB.Data;

public class GiftCard
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUsed { get; set; }

    public string? UsedByUserId { get; set; }

    public DateTime? UsedAt { get; set; }
}