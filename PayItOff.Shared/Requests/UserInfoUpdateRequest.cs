namespace PayItOff.Shared.Requests
{
    public class UserInfoUpdateRequest
    {
        public required string Nickname { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? IBAN { get; set; } = string.Empty;
    }
}
