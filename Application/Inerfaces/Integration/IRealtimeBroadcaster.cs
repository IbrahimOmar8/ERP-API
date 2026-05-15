namespace Application.Inerfaces.Integration
{
    // Optional in-process broadcaster — implemented by the API layer (SignalR).
    // Service layer can use this without taking a hard dependency on SignalR.
    public interface IRealtimeBroadcaster
    {
        Task BroadcastAsync(string @event, object data);
    }

    // No-op fallback for tests / when SignalR is not configured.
    public class NullRealtimeBroadcaster : IRealtimeBroadcaster
    {
        public Task BroadcastAsync(string @event, object data) => Task.CompletedTask;
    }
}
