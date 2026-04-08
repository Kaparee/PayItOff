#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class InvalidPasswordException : PayItOffException
{
    public InvalidPasswordException()
        : base("Podano nieprawidłowe hasło.")
    {
    }
}