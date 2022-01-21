#pragma warning disable 8618
using BigMission.CommandTools;
using Microsoft.AspNetCore.SignalR.Client;
using NLog.Config;
using NLog.Layouts;

namespace NLog.Targets.ServiceHub
{
    [Target("ServiceHub")]
    public class ServiceHubTarget : TargetWithLayout
    {
        [RequiredParameter]
        public Layout SourceKey { get; set; }
        [RequiredParameter]
        public Layout HubUrl { get; set; }
        [RequiredParameter]
        public Layout ApiKey { get; set; }

        private HubConnection hubConnection;


        protected override void InitializeTarget()
        {
            var deviceSourceStr = SourceKey?.Render(LogEventInfo.CreateNullEvent());
                var url = HubUrl?.Render(LogEventInfo.CreateNullEvent());
            if (Guid.TryParse(deviceSourceStr, out Guid deviceSourceKey) && !string.IsNullOrWhiteSpace(url))
            {
                var apiKey = ApiKey?.Render(LogEventInfo.CreateNullEvent());

                hubConnection = new HubConnectionBuilder()
                    .WithUrl(url, option =>
                    {
                        option.AccessTokenProvider = async () =>
                        {
                            var token = KeyUtilities.EncodeToken(deviceSourceKey, apiKey);
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

        protected override void CloseTarget()
        {
            hubConnection.DisposeAsync();

            base.CloseTarget();
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            var sourceKey = SourceKey.Render(logEvent);
            var lm = new LogMessage { Message = message, SourceKey = new Guid(sourceKey) };

            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    hubConnection.StartAsync().Wait();
                }
                catch { }
            }

            if (hubConnection.State == HubConnectionState.Connected)
            {
                hubConnection.SendAsync("PostLogMessage", lm).Wait();
            }
        }
    }
}