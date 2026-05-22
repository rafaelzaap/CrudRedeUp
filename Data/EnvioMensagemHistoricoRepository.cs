using Dapper;
using EvolutionSender.Models;
using MySqlConnector;

namespace EvolutionSender.Data;

public class EnvioMensagemHistoricoRepository : IEnvioMensagemHistoricoRepository
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _schemaVerificado;
    private readonly string _connectionString;

    public EnvioMensagemHistoricoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task RegistrarAsync(EnvioMensagemHistorico historico, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            INSERT INTO envios_mensagens
                (mensagem_id, membro_codigo, membro_nome, telefone_original, telefone_normalizado, status, erro)
            VALUES
                (@MensagemId, @MembroCodigo, @MembroNome, @TelefoneOriginal, @TelefoneNormalizado, @Status, @Erro);
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, historico, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<EnvioMensagemHistorico>> ListarRecentesAsync(
        int quantidade,
        int? mensagemId = null,
        CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                mensagem_id AS MensagemId,
                membro_codigo AS MembroCodigo,
                membro_nome AS MembroNome,
                telefone_original AS TelefoneOriginal,
                telefone_normalizado AS TelefoneNormalizado,
                status AS Status,
                erro AS Erro,
                criado_em AS CriadoEm
            FROM envios_mensagens
            WHERE (@MensagemId IS NULL OR mensagem_id = @MensagemId)
            ORDER BY criado_em DESC, id DESC
            LIMIT @Quantidade;
            """;

        var historico = await connection.QueryAsync<EnvioMensagemHistorico>(
            new CommandDefinition(
                sql,
                new { Quantidade = Math.Clamp(quantidade, 1, 200), MensagemId = mensagemId },
                cancellationToken: cancellationToken));

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
                CREATE TABLE IF NOT EXISTS envios_mensagens (
                    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    mensagem_id INT NOT NULL,
                    membro_codigo INT NOT NULL,
                    membro_nome VARCHAR(150) NOT NULL,
                    telefone_original VARCHAR(30) NOT NULL,
                    telefone_normalizado VARCHAR(30) NOT NULL,
                    status VARCHAR(30) NOT NULL,
                    erro TEXT NULL,
                    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (id),
                    INDEX ix_envios_mensagem_criado (mensagem_id, criado_em),
                    INDEX ix_envios_status_criado (status, criado_em)
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
