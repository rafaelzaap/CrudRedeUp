using Dapper;
using MySqlConnector;

namespace EvolutionSender.Data;

public class DatabaseStartupMigration : IDatabaseStartupMigration
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseStartupMigration> _logger;

    public DatabaseStartupMigration(IConfiguration configuration, ILogger<DatabaseStartupMigration> logger)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var tabelaAntigaExiste = await TabelaExisteAsync(connection, "menbros", cancellationToken);
            var tabelaNovaExiste = await TabelaExisteAsync(connection, "membros", cancellationToken);

            if (tabelaAntigaExiste && !tabelaNovaExiste)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "RENAME TABLE menbros TO membros;",
                    cancellationToken: cancellationToken));

                _logger.LogInformation("Tabela menbros renomeada para membros.");
            }
            else if (tabelaAntigaExiste && tabelaNovaExiste)
            {
                _logger.LogWarning(
                    "As tabelas menbros e membros existem ao mesmo tempo. Revise o banco antes de remover a tabela antiga.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nao foi possivel executar a migracao inicial do banco.");
        }
    }

    private static async Task<bool> TabelaExisteAsync(
        MySqlConnection connection,
        string nome,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = @Nome;
            """;

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Nome = nome }, cancellationToken: cancellationToken));

        return total > 0;
    }
}
