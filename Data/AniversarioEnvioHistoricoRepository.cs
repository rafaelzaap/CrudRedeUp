using Dapper;
using EvolutionSender.Models;
using MySqlConnector;

namespace EvolutionSender.Data;

public class AniversarioEnvioHistoricoRepository : IAniversarioEnvioHistoricoRepository
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _schemaVerificado;
    private readonly string _connectionString;

    public AniversarioEnvioHistoricoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task<bool> JaEnviadoAsync(
        int membroCodigo,
        string tipo,
        DateTime dataReferencia,
        CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT COUNT(*)
            FROM aniversarios_envios
            WHERE membro_codigo = @MembroCodigo
              AND tipo = @Tipo
              AND data_referencia = @DataReferencia
              AND status = 'enviado';
            """;

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                MembroCodigo = membroCodigo,
                Tipo = tipo,
                DataReferencia = dataReferencia.Date
            }, cancellationToken: cancellationToken));

        return total > 0;
    }

    public async Task RegistrarAsync(AniversarioEnvioHistorico historico, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            INSERT INTO aniversarios_envios
                (membro_codigo, tipo, data_referencia, telefone_destino, status, erro)
            VALUES
                (@MembroCodigo, @Tipo, @DataReferencia, @TelefoneDestino, @Status, @Erro);
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, historico, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AniversarioEnvioHistorico>> ListarRecentesAsync(
        int quantidade = 20,
        CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                membro_codigo AS MembroCodigo,
                tipo AS Tipo,
                data_referencia AS DataReferencia,
                telefone_destino AS TelefoneDestino,
                status AS Status,
                erro AS Erro,
                criado_em AS CriadoEm
            FROM aniversarios_envios
            ORDER BY criado_em DESC, id DESC
            LIMIT @Quantidade;
            """;

        var historico = await connection.QueryAsync<AniversarioEnvioHistorico>(
            new CommandDefinition(sql, new { Quantidade = Math.Clamp(quantidade, 1, 100) }, cancellationToken: cancellationToken));

        return historico.ToList();
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
                CREATE TABLE IF NOT EXISTS aniversarios_envios (
                    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    membro_codigo INT NOT NULL,
                    tipo VARCHAR(40) NOT NULL,
                    data_referencia DATE NOT NULL,
                    telefone_destino VARCHAR(30) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    erro TEXT NULL,
                    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (id),
                    INDEX ix_aniversarios_envios_membro_tipo_data (membro_codigo, tipo, data_referencia),
                    INDEX ix_aniversarios_envios_criado (criado_em)
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
