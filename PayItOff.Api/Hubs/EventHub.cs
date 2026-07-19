using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace PayItOff.Api.Hubs
{
    public class EventHub : Hub
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte>> GroupPresence = new();

        public async Task JoinGroup(string groupId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            var groupUsers = GroupPresence.GetOrAdd(groupId, _ => new ConcurrentDictionary<int, byte>());

            groupUsers.TryAdd(userId, 0);

            var allUsersIds = groupUsers.Keys.ToArray();
            await Clients.Caller.SendAsync("ReceiveInitialPresence", allUsersIds);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);

            await Clients.OthersInGroup(groupId).SendAsync("ReceiveUserPresence", userId, true);
        }

        public async Task LeaveGroup(string groupId)
        {
            var userId = int.Parse(Context.UserIdentifier!);

            if (GroupPresence.TryGetValue(groupId, out var groupUsers))
            {
                groupUsers.TryRemove(userId, out _);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);

            await Clients.Group(groupId).SendAsync("ReceiveUserPresence", userId, false);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.UserIdentifier != null)
            {
                var userId = int.Parse(Context.UserIdentifier);

                foreach (var group in GroupPresence)
                {
                    if (group.Value.TryRemove(userId, out _))
                    {
                        await Clients.Group(group.Key).SendAsync("ReceiveUserPresence", userId, false);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}