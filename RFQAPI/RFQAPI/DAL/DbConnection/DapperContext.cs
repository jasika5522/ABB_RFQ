using Npgsql;

namespace RFQAPI.DAL.DbConnection;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RfqDb")
            ?? throw new InvalidOperationException("Connection string 'RfqDb' was not found.");
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}