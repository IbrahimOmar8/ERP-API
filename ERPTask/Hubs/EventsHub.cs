using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ERPTask.Hubs
{
    // Live event broadcast — clients receive named messages like
    //   "sale.created", "stock.low", "eta.failed".
    [Authorize]
    public class EventsHub : Hub
    {
        // Client can join role-/warehouse-specific groups in the future.
        public Task Ping() => Clients.Caller.SendAsync("pong", DateTime.UtcNow);
    }
}
