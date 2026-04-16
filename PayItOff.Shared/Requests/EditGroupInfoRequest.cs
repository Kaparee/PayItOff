namespace PayItOff.Shared.Requests
{
    public class EditGroupInfoRequest
    {
        public required int GroupId { get; set; }
        public string? NewName { get; set; }
    }
}
