#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class InvalidUserRoleException : PayItOffException
{
    public InvalidUserRoleException()
        : base("Użytkownik nie posiada odpowiedniej roli dla tego działania!")
    {
    }
}