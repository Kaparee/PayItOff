namespace PayItOff.Domain.Exceptions;

public class InvalidPasswordException : PayItOffException
{
    public InvalidPasswordException()
        : base("Podano nieprawidłowe hasło.")
    {
    }
}