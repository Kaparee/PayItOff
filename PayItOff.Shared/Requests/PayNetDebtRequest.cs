namespace PayItOff.Shared.Requests;
public sealed class PayNetDebtRequest
{
    public int CreditorId { get; set; }
    public decimal Amount { get; set; }
}
