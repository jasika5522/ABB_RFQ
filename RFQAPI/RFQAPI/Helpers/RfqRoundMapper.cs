using RFQAPI.Models.DTOs.Rounds;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class RfqRoundMapper
{
    public static RfqRoundResponse ToResponse(RfqRound round) => new()
    {
        RoundId = round.RoundId,
        RfqId = round.RfqId,
        RoundNumber = round.RoundNumber,
        Status = round.Status,
        PublishedAt = round.PublishedAt,
        Deadline = round.Deadline,
        ClosedAt = round.ClosedAt,
        CreatedAt = round.CreatedAt
    };
}