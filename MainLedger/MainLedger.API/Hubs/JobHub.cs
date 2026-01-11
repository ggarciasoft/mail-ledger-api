using Microsoft.AspNetCore.SignalR;

namespace MainLedger.API.Hubs;

/// <summary>
/// SignalR hub for real-time job status updates.
/// </summary>
public class JobHub : Hub
{
    /// <summary>
    /// Adds the connection to a user-specific group.
    /// </summary>
    public async Task JoinUserGroup(Guid userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    /// <summary>
    /// Removes the connection from a user-specific group.
    /// </summary>
    public async Task LeaveUserGroup(Guid userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }
}
