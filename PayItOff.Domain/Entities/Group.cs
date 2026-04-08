namespace PayItOff.Domain.Entities
{
    public class Group
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string AvatarUrl { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected Group() { }
        private Group(string name, string avatarUrl)
        {
            if(string.IsNullOrWhiteSpace(name)) { throw new ArgumentException(nameof(name)); }
            if(string.IsNullOrWhiteSpace(avatarUrl)) { throw new ArgumentException(nameof(avatarUrl)); }

            Name = name;
            AvatarUrl = avatarUrl;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Group Create(string name, string avatarUrl)
        {
            var finalAvatar = string.IsNullOrWhiteSpace(avatarUrl) ? "default_avatar.jpg" : avatarUrl;
            return new Group(name, finalAvatar);
        }

        public void Edit(string? newName, string? newAvatarUrl)
        {
            if (string.IsNullOrWhiteSpace(newName)) { throw new ArgumentException(nameof(newName)); }
            if (string.IsNullOrWhiteSpace(newAvatarUrl)) { throw new ArgumentException(nameof(newAvatarUrl)); }

            Name = newName;
            AvatarUrl = newAvatarUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete()
        {
            UpdatedAt = DateTime.UtcNow;
            DeletedAt = DateTime.UtcNow;
        }
    }
}