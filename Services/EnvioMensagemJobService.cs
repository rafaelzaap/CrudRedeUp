using System.Collections.Concurrent;

namespace EvolutionSender.Services;

public class EnvioMensagemJobService : IEnvioMensagemJobService
{
    private readonly IEnvioMensagemJobQueue _queue;
    private readonly ConcurrentDictionary<Guid, EnvioMensagemJob> _jobs = new();

    public EnvioMensagemJobService(IEnvioMensagemJobQueue queue)
    {
        _queue = queue;
    }

    public EnvioMensagemJob Enfileirar(int? mensagemId)
    {
        var job = new EnvioMensagemJob { MensagemId = mensagemId };
        _jobs[job.Id] = job;
        _queue.EnfileirarAsync(job.Id).GetAwaiter().GetResult();
        return job;
    }

    public IReadOnlyList<EnvioMensagemJob> ListarRecentes(int quantidade = 10, int? mensagemId = null)
    {
        return _jobs.Values
            .Where(job => mensagemId is null || job.MensagemId == mensagemId)
            .OrderByDescending(job => job.CriadoEm)
            .Take(Math.Clamp(quantidade, 1, 50))
            .ToList();
    }

    internal bool TentarObter(Guid id, out EnvioMensagemJob? job)
    {
        return _jobs.TryGetValue(id, out job);
    }
}
