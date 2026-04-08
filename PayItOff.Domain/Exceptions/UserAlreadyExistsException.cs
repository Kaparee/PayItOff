namespace PayItOff.Domain.Exceptions;

public class UserAlreadyExistsException : PayItOffException
{
    public UserAlreadyExistsException(string field, string value)
        : base($"Wartość '{value}' dla pola '{field}' jest już zajęta.")
    {
    }
}