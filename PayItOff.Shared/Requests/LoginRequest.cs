namespace PayItOff.Shared.Requests
{
    public class LoginRequest
    {
        public required string EmailOrNickname { get; set; }
        public required string Password { get; set; }
    }
}
