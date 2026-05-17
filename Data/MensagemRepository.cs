using Dapper;
using EvolutionSender.Models;
using MySqlConnector;

namespace EvolutionSender.Data;

public class MensagemRepository : IMensagemRepository
{
    private readonly string _connectionString;

    public MensagemRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task<IReadOnlyList<Mensagem>> ListarAsync(string? busca, bool incluirInativas)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                titulo AS Titulo,
                ocasiao AS Ocasiao,
                mensagem AS Texto,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM mensagens
            WHERE (@IncluirInativas = TRUE OR ativo = 1)
              AND (
                    @Busca IS NULL
                    OR titulo LIKE CONCAT('%', @Busca, '%')
                    OR ocasiao LIKE CONCAT('%', @Busca, '%')
                    OR mensagem LIKE CONCAT('%', @Busca, '%')
                  )
            ORDER BY criado_em DESC, id DESC;
            """;

        var mensagens = await connection.QueryAsync<Mensagem>(sql, new
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            IncluirInativas = incluirInativas
        });

        return mensagens.ToList();
    }

    public async Task<Mensagem?> ObterPorIdAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                titulo AS Titulo,
                ocasiao AS Ocasiao,
                mensagem AS Texto,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM mensagens
            WHERE id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<Mensagem>(sql, new { Id = id });
    }

    public async Task<Mensagem?> ObterAtivaAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                titulo AS Titulo,
                ocasiao AS Ocasiao,
                mensagem AS Texto,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM mensagens
            WHERE ativo = 1
            ORDER BY criado_em DESC, id DESC
            LIMIT 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<Mensagem>(sql);
    }

    public async Task<IReadOnlyList<Mensagem>> ListarAtivasAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                titulo AS Titulo,
                ocasiao AS Ocasiao,
                mensagem AS Texto,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM mensagens
            WHERE ativo = 1
            ORDER BY atualizado_em ASC, criado_em ASC, id ASC;
            """;

        var mensagens = await connection.QueryAsync<Mensagem>(sql);
        return mensagens.ToList();
    }

    public async Task<Mensagem?> ObterAtivaPorIdAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                id AS Id,
                titulo AS Titulo,
                ocasiao AS Ocasiao,
                mensagem AS Texto,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM mensagens
            WHERE ativo = 1
              AND id = @Id
            ORDER BY atualizado_em DESC
            LIMIT 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<Mensagem>(sql, new { Id = id });
    }

    public async Task<int> CriarAsync(Mensagem mensagem)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            INSERT INTO mensagens
                (titulo, ocasiao, mensagem, ativo)
            VALUES
                (@Titulo, @Ocasiao, @Texto, @Ativo);

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(sql, mensagem);
    }

    public async Task<bool> AtualizarAsync(Mensagem mensagem)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            UPDATE mensagens
            SET
                titulo = @Titulo,
                ocasiao = @Ocasiao,
                mensagem = @Texto,
                ativo = @Ativo,
                atualizado_em = CURRENT_TIMESTAMP
            WHERE id = @Id;
            """;

        var linhas = await connection.ExecuteAsync(sql, mensagem);
        return linhas > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        return await InativarAsync(id);
    }

    public async Task<bool> InativarAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            UPDATE mensagens
            SET ativo = 0,
                atualizado_em = CURRENT_TIMESTAMP
            WHERE id = @Id;
            """;

        var linhas = await connection.ExecuteAsync(sql, new { Id = id });
        return linhas > 0;
    }
}
