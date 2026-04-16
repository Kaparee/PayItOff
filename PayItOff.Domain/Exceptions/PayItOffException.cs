#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public abstract class PayItOffException : Exception
{
    protected PayItOffException(string message) : base(message)
    {
    }
}