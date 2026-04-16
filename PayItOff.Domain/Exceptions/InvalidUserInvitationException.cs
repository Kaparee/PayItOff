#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class InvalidUserInvitationException : PayItOffException
{
    public InvalidUserInvitationException()
        : base("Podany użytkownik nie posiada zaproszenia o podanych parametrach.")
    {
    }
}