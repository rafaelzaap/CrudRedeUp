using Dapper;
using MySqlConnector;

namespace EvolutionSender.Data;

public class MySqlEnvioMensagemLock : IEnvioMensagemLock
{
    private const string LockName = "EvolutionSender:EnvioMensagem";
    private readonly string _connectionString;

    public MySqlEnvioMensagemLock(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedeUp")
            ?? throw new InvalidOperationException("Configure a connection string 'RedeUp' no appsettings.json.");
    }

    public async Task<IAsyncDisposable?> TentarAdquirirAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var adquirido = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT GET_LOCK(@Nome, 0);",
                new { Nome = LockName },
                cancellationToken: cancellationToken));

        if (adquirido == 1)
        {
            return new LockHandle(connection);
        }

        await connection.DisposeAsync();
        return null;
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly MySqlConnection _connection;

        public LockHandle(MySqlConnection connection)
        {
            _connection = connection;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition("SELECT RELEASE_LOCK(@Nome);", new { Nome = LockName }));
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
