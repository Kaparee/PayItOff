using PayItOff.Domain.Enums;

namespace PayItOff.Domain.Entities
{
    public class GroupMember
    {
        public int Id { get; private set; }
        public User? User { get; private set; }
        public Group? Group { get; private set; }
        public int UserId { get; private set; }
        public int GroupId { get; private set; }
        public bool IsFavorite { get; private set; }
        public GroupMemberStatus Status { get; private set; }
        public GroupMemberRole Role { get; private set; }
        public DateTime InvitedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? JoinedAt { get; private set; }
        public DateTime? LeftAt { get; private set; }

        protected GroupMember() { }

        private GroupMember(User user, Group group, bool isFavorite, GroupMemberStatus status, GroupMemberRole role, DateTime? joinedAt)
        {
            if (user == null) { throw new ArgumentException(nameof(user)); }
            if (group == null) { throw new ArgumentException(nameof(group)); }

            User = user;
            Group = group;
            UserId = user.Id;
            GroupId = group.Id;
            IsFavorite = isFavorite;
            Status = status;
            Role = role;
            InvitedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            JoinedAt = joinedAt;
        }

        public static GroupMember Invite(User user, Group group, GroupMemberRole role)
        {
            if (user == null) { throw new ArgumentException(nameof(user)); }
            if (group == null) { throw new ArgumentException(nameof(group)); }

            return new GroupMember(user, group, false, GroupMemberStatus.Pending, role, null);
        }

        public static GroupMember CreateOwner(User user, Group group)
        {
            if (user == null) { throw new ArgumentException(nameof(user)); }
            if (group == null) { throw new ArgumentException(nameof(group)); }

            return new GroupMember(user, group, false, GroupMemberStatus.Accepted, GroupMemberRole.Owner, DateTime.UtcNow);
        }

        public void Accept()
        {
            if (Status == GroupMemberStatus.Pending)
            {
                Status = GroupMemberStatus.Accepted;
                JoinedAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                throw new Exception("Tu będzie exception dla złego statusu akceptu");
            }
        }

        public void Decline()
        {
            if (Status == GroupMemberStatus.Pending)
            {
                Status = GroupMemberStatus.Declined;
                JoinedAt = null;
                Role = GroupMemberRole.Member;
                UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                throw new Exception("Tu będzie exception dla złego statusu declined");
            }
        }

        public void UpdateRole(GroupMemberRole newRole)
        {
            if (Status == GroupMemberStatus.Accepted)
            {
                Role = newRole;
                UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                throw new Exception("Tu będzie exception dla złego statusu role");
            }
        }

        public void ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Leave()
        {
            if (Status == GroupMemberStatus.Accepted && Role != GroupMemberRole.Owner)
            {
                Status = GroupMemberStatus.Left;
                LeftAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                throw new Exception("Tu będzie exception dla złego statusu role albo że ma sie ownera");
            }
        }

        public void Kick()
        {
            if (Status == GroupMemberStatus.Accepted && Role != GroupMemberRole.Owner)
            {
                Status = GroupMemberStatus.Kicked;
                LeftAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                throw new Exception("Tu będzie exception dla złego statusu");
            }
        }
    }
}