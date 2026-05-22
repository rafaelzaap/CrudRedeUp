using System.Threading.Channels;

namespace EvolutionSender.Services;

public class EnvioMensagemJobQueue : IEnvioMensagemJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public async Task EnfileirarAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(jobId, cancellationToken);
    }

    public ValueTask<Guid> AguardarProximoAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
