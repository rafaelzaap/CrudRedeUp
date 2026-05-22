using Dapper;
using MySqlConnector;

namespace EvolutionSender.Data;

public class SistemaConfiguracaoRepository : ISistemaConfiguracaoRepository
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _schemaVerificado;
    private readonly string _connectionString;

    public SistemaConfiguracaoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task<string?> ObterAsync(string chave, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = "SELECT valor FROM sistema_configuracoes WHERE chave = @Chave LIMIT 1;";
        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { Chave = chave }, cancellationToken: cancellationToken));
    }

    public async Task SalvarAsync(string chave, string valor, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            INSERT INTO sistema_configuracoes (chave, valor)
            VALUES (@Chave, @Valor)
            ON DUPLICATE KEY UPDATE
                valor = VALUES(valor),
                atualizado_em = CURRENT_TIMESTAMP;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Chave = chave, Valor = valor }, cancellationToken: cancellationToken));
    }

    private async Task GarantirSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaVerificado)
        {
            return;
        }

        await SchemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_schemaVerificado)
            {
                return;
            }

            await using var connection = new MySqlConnection(_connectionString);
            const string sql = """
                CREATE TABLE IF NOT EXISTS sistema_configuracoes (
                    chave VARCHAR(120) NOT NULL,
                    valor TEXT NOT NULL,
                    atualizado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (chave)
                );
                """;

            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            _schemaVerificado = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }
}
