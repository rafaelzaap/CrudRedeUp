namespace EvolutionSender.Data;

public interface IDatabaseStartupMigration
{
    Task ExecutarAsync(CancellationToken cancellationToken = default);
}
