using System;
using System.Collections.Generic;
using System.Text;

namespace PayItOff.Application.Interfaces
{
    public interface IRealTimeNotificationService
    {
        Task SendInvitationEventAsync(int targetUserId);
        Task SendExpenseUpdateEventAsync(int groupId);
        Task SendUserKickedEventAsync(int userId, int groupId);
        Task SendSettlementUpdateEventAsync(int user1Id, int user2Id);
        Task SendGroupUpdateEventAsync(int groupId);
        Task SendFriendUpdateEventAsync(int targetUserId);
        Task SendSystemNotificationEventAsync(int userId);
    }
}
