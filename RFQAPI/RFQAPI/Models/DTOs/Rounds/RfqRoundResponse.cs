namespace RFQAPI.Models.DTOs.Rounds;

public class RfqRoundResponse
{
    public long RoundId { get; set; }
    public long RfqId { get; set; }
    public int RoundNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}