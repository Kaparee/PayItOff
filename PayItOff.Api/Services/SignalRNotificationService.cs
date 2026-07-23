using Microsoft.AspNetCore.SignalR;
using PayItOff.Api.Hubs;
using PayItOff.Application.Interfaces;

namespace PayItOff.Api.Services
{
    public class SignalRNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<EventHub> _hubContext;

        public SignalRNotificationService(IHubContext<EventHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendInvitationEventAsync(int targetUserId)
        {
            await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("ReceiveInvitation");
        }

        public async Task SendExpenseUpdateEventAsync(int groupId)
        {
            await _hubContext.Clients.Group(groupId.ToString()).SendAsync("ReceiveExpenseUpdate");
        }

        public async Task SendUserKickedEventAsync(int userId, int groupId)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveKick", groupId);
        }

        public async Task SendSettlementUpdateEventAsync(int user1Id, int user2Id)
        {
            await _hubContext.Clients.Users(new[] { user1Id.ToString(), user2Id.ToString() }).SendAsync("ReceiveSettlementUpdate");
        }

        public async Task SendGroupUpdateEventAsync(int groupId)
        {
            await _hubContext.Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupUpdate");
        }

        public async Task SendFriendUpdateEventAsync(int targetUserId)
        {
            await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("ReceiveFriendUpdate");
        }

        public async Task SendSystemNotificationEventAsync(int userId)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveSystemNotification");
        }
    }
}
