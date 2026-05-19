namespace PayItOff.Shared.Requests;

public class RemindDebtRequest
{
    public int GroupId { get; set; }
    public int DebtorUserId { get; set; }
}
