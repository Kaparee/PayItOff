#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class FriendInvitationAlreadyExistsException : PayItOffException
{
    public FriendInvitationAlreadyExistsException()
        : base($"Zaproszenie do podanego użytkownika już istnieje!")
    {
    }
}