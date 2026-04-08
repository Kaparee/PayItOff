namespace PayItOff.Domain.Exceptions;

public class UserNotActiveOrVerifiedException : PayItOffException
{
    public UserNotActiveOrVerifiedException()
        : base($"Konto jest nieaktywne lub niezweryfikowane.")
    {
    }
}