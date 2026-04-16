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
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException(nameof(name)); }
            if (string.IsNullOrWhiteSpace(avatarUrl)) { throw new ArgumentException(nameof(avatarUrl)); }

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
            bool isUpdated = false;
            if (!string.IsNullOrWhiteSpace(newName) && Name != newName)
            {
                Name = newName;
                isUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(newAvatarUrl) && AvatarUrl != newAvatarUrl)
            {
                AvatarUrl = newAvatarUrl;
                isUpdated = true;
            }

            if (isUpdated)
            {
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void Delete()
        {
            UpdatedAt = DateTime.UtcNow;
            DeletedAt = DateTime.UtcNow;
        }
    }
}