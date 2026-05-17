namespace EvolutionSender.Services;

public class EvolutionApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "apikey";
    public string SendTextEndpointTemplate { get; set; } = "/message/sendText/{instance}";
}
