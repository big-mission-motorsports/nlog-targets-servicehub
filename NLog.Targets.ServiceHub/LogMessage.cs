using Newtonsoft.Json;

namespace BigMission.NLog.Targets.ServiceHub;

public class LogMessage
{
    [JsonProperty("sk")]
    public Guid SourceKey { get; set; }
    [JsonProperty("m")]
    public string? Message { get; set; }
}
