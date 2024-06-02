#pragma warning disable 8618
using BigMission.CommandTools;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace BigMission.NLog.Targets.ServiceHub;

[Target("ServiceHub")]
public class ServiceHubTarget : TargetWithLayout
{
    [RequiredParameter]
    public Layout SourceKey { get; set; }
    [RequiredParameter]
    public Layout HubUrl { get; set; }
    [RequiredParameter]
    public Layout ApiKey { get; set; }
    [RequiredParameter]
    public Layout AesKey { get; set; }

    private HubConnection hubConnection;


    protected override void InitializeTarget()
    {
        var deviceSourceStr = SourceKey?.Render(LogEventInfo.CreateNullEvent());
            var url = HubUrl?.Render(LogEventInfo.CreateNullEvent());
        if (Guid.TryParse(deviceSourceStr, out Guid deviceSourceKey) && !string.IsNullOrWhiteSpace(url))
        {
            var apiKey = ApiKey?.Render(LogEventInfo.CreateNullEvent());
            var aesKey = AesKey?.Render(LogEventInfo.CreateNullEvent());

            hubConnection = new HubConnectionBuilder()
                .WithUrl(url, option =>
                {
                    option.AccessTokenProvider = async () =>
                    {
                        var token = KeyUtilities.EncodeToken(deviceSourceKey, apiKey, aesKey);
                        return await Task.FromResult(token);
                    };
                }).Build();

            base.InitializeTarget();
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    protected override async void CloseTarget()
    {
        await hubConnection.DisposeAsync();

        base.CloseTarget();
    }

    protected override async void Write(LogEventInfo logEvent)
    {
        var message = Layout.Render(logEvent);
        var sourceKey = SourceKey.Render(logEvent);
        var lm = new LogMessage { Message = message, SourceKey = new Guid(sourceKey) };

        if (hubConnection.State == HubConnectionState.Disconnected && hubConnection.State != HubConnectionState.Connecting)
        {
            try
            {
                await hubConnection.StartAsync();
            }
            catch { }
        }

        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.SendAsync("PostLogMessage", lm);
        }
    }
}