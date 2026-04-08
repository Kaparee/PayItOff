
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Requests
{
    public class InviteUserRequest
    {
        public required int UserId { get; set; }
        public required int GroupId { get; set; }
        public required GroupMemberStatus Status { get; set; }
    }
}
