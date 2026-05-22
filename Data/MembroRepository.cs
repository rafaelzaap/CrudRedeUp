using Dapper;
using EvolutionSender.Models;
using MySqlConnector;

namespace EvolutionSender.Data;

public class MembroRepository : IMembroRepository
{
    private readonly string _connectionString;

    public MembroRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task<IReadOnlyList<Membro>> ListarAsync(string? busca, bool incluirInativos)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                codigo AS Codigo,
                nome AS Nome,
                data_de_nascimento AS DataDeNascimento,
                telefone AS Telefone,
                observacao AS Observacao,
                primeiro_nome AS PrimeiroNome,
                ativo AS Ativo
            FROM membros
            WHERE CODIGO = 37
              AND (@IncluirInativos = TRUE OR ativo = 1)
              AND (
                    @Busca IS NULL
                    OR nome LIKE CONCAT('%', @Busca, '%')
                    OR primeiro_nome LIKE CONCAT('%', @Busca, '%')
                    OR telefone LIKE CONCAT('%', @Busca, '%')
                  )
            ORDER BY nome;
            """;

        var membros = await connection.QueryAsync<Membro>(sql, new
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            IncluirInativos = incluirInativos
        });

        return membros.ToList();
    }

    public async Task<Membro?> ObterPorCodigoAsync(int codigo)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                codigo AS Codigo,
                nome AS Nome,
                data_de_nascimento AS DataDeNascimento,
                telefone AS Telefone,
                observacao AS Observacao,
                primeiro_nome AS PrimeiroNome,
                ativo AS Ativo
            FROM membros
            WHERE codigo = @Codigo;
            """;

        return await connection.QuerySingleOrDefaultAsync<Membro>(sql, new { Codigo = codigo });
    }

    public async Task<IReadOnlyList<Membro>> ListarAniversariantesAsync(bool incluirInativos = false)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            SELECT
                codigo AS Codigo,
                nome AS Nome,
                data_de_nascimento AS DataDeNascimento,
                telefone AS Telefone,
                observacao AS Observacao,
                primeiro_nome AS PrimeiroNome,
                ativo AS Ativo
            FROM membros
            WHERE (@IncluirInativos = TRUE OR ativo = 1)
            ORDER BY MONTH(data_de_nascimento), DAY(data_de_nascimento), nome;
            """;

        var membros = await connection.QueryAsync<Membro>(sql, new
        {
            IncluirInativos = incluirInativos
        });

        return membros.ToList();
    }

    public async Task<int> CriarAsync(Membro membro)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            INSERT INTO membros
                (nome, data_de_nascimento, telefone, observacao, ativo)
            VALUES
                (@Nome, @DataDeNascimento, @Telefone, @Observacao, @Ativo);

            SELECT LAST_INSERT_ID();
            """;

        return await connection.ExecuteScalarAsync<int>(sql, membro);
    }

    public async Task<bool> AtualizarAsync(Membro membro)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = """
            UPDATE membros
            SET
                nome = @Nome,
                data_de_nascimento = @DataDeNascimento,
                telefone = @Telefone,
                observacao = @Observacao,
                ativo = @Ativo
            WHERE codigo = @Codigo;
            """;

        var linhas = await connection.ExecuteAsync(sql, membro);
        return linhas > 0;
    }

    public async Task<bool> ExcluirAsync(int codigo)
    {
        await using var connection = new MySqlConnection(_connectionString);

        const string sql = "UPDATE membros SET ativo = 0 WHERE codigo = @Codigo;";
        var linhas = await connection.ExecuteAsync(sql, new { Codigo = codigo });
        return linhas > 0;
    }
}
