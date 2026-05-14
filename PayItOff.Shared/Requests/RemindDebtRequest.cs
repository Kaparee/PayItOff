namespace PayItOff.Shared.Requests;

public class RemindDebtRequest
{
    public int GroupId { get; set; }
    /// <summary>Dłużnik (komu wysyłamy przypomnienie o zapłacie).</summary>
    public int DebtorUserId { get; set; }
}
