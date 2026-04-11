namespace PayItOff.Shared.Requests
{
    public class ModifyPasswordRequest
    {
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}