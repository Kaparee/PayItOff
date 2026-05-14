namespace PayItOff.Domain.Exceptions;

public sealed class SettlementOperationException : PayItOffException
{
    public SettlementOperationException(string message) : base(message)
    {
    }
}
