#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class UserNotFoundException : PayItOffException
{
    public UserNotFoundException()
        : base($"Użytkownik o podanych danych nie istnieje.")
    {
    }
}