using Dapper;
using Npgsql;

var cs = "Host=localhost;Port=5432;Database=abb_hackathon;Username=postgres;Password=AbbRfq@2026Secure;";
await using var conn = new NpgsqlConnection(cs);
await conn.OpenAsync();
var rfqId = args.Length > 0 ? long.Parse(args[0]) : 9L;
var roundId = args.Length > 1 ? long.Parse(args[1]) : 7L;
var rows = await conn.QueryAsync(
    "SELECT * FROM get_ranked_bids(@rfq, @round)",
    new { rfq = rfqId, round = roundId });
foreach (var row in rows)
{
    Console.WriteLine(row);
}