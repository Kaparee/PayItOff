namespace PayItOff.Shared.Responses;

public class PayableDebtOptionResponse
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CreditorId { get; set; }
    public string CreditorName { get; set; } = string.Empty;
    public string CreditorSurname { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLine => $"{CreditorName} {CreditorSurname} — {GroupName} (max {Amount:N2} zł)";
}
