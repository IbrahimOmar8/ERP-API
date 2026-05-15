using ERPTask.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ERPTask.Services
{
    // Hooks the existing WebhookService dispatcher: every webhook event
    // also fans out to connected SignalR clients via the same event name.
    public class RealtimeBroadcaster
    {
        private readonly IHubContext<EventsHub> _hub;
        public RealtimeBroadcaster(IHubContext<EventsHub> hub) => _hub = hub;

        public Task BroadcastAsync(string @event, object data) =>
            _hub.Clients.All.SendAsync(@event, data);
    }
}
