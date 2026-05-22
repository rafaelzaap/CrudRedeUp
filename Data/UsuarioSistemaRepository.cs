using Dapper;
using EvolutionSender.Models;
using MySqlConnector;

namespace EvolutionSender.Data;

public class UsuarioSistemaRepository : IUsuarioSistemaRepository
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static bool _schemaVerificado;
    private readonly string _connectionString;

    public UsuarioSistemaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task<bool> ExisteUsuarioAsync(CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM usuarios_sistema;", cancellationToken: cancellationToken));

        return total > 0;
    }

    public async Task<UsuarioSistema?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                nome AS Nome,
                email AS Email,
                senha_hash AS SenhaHash,
                ativo AS Ativo,
                tentativas_falhas AS TentativasFalhas,
                bloqueado_ate AS BloqueadoAte,
                criado_em AS CriadoEm,
                ultimo_login_em AS UltimoLoginEm
            FROM usuarios_sistema
            WHERE email = @Email
            LIMIT 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<UsuarioSistema>(
            new CommandDefinition(sql, new { Email = email.Trim().ToLowerInvariant() }, cancellationToken: cancellationToken));
    }

    public async Task<int> CriarAsync(UsuarioSistema usuario, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            INSERT INTO usuarios_sistema
                (nome, email, senha_hash, ativo)
            VALUES
                (@Nome, @Email, @SenhaHash, @Ativo);

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, usuario, cancellationToken: cancellationToken));
    }

    public async Task RegistrarLoginSucessoAsync(int id, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            UPDATE usuarios_sistema
            SET tentativas_falhas = 0,
                bloqueado_ate = NULL,
                ultimo_login_em = CURRENT_TIMESTAMP
            WHERE id = @Id;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task RegistrarFalhaLoginAsync(int id, DateTime? bloquearAte, CancellationToken cancellationToken = default)
    {
        await GarantirSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            UPDATE usuarios_sistema
            SET tentativas_falhas = tentativas_falhas + 1,
                bloqueado_ate = @BloquearAte
            WHERE id = @Id;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, BloquearAte = bloquearAte }, cancellationToken: cancellationToken));
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
                CREATE TABLE IF NOT EXISTS usuarios_sistema (
                    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
                    nome VARCHAR(120) NOT NULL,
                    email VARCHAR(190) NOT NULL,
                    senha_hash VARCHAR(500) NOT NULL,
                    ativo TINYINT NOT NULL DEFAULT 1,
                    tentativas_falhas INT NOT NULL DEFAULT 0,
                    bloqueado_ate DATETIME NULL,
                    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ultimo_login_em DATETIME NULL,
                    PRIMARY KEY (id),
                    UNIQUE KEY ux_usuarios_sistema_email (email)
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
