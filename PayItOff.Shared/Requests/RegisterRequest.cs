namespace PayItOff.Shared.Requests
{
    public class RegisterRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Nickname { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public string AvatarUrl { get; set; } = "default-avatar.jpg";
        public string PhoneNumber { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
    }
}
