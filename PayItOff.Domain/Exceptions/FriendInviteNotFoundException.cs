#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class FriendInviteNotFoundException : PayItOffException
{
    public FriendInviteNotFoundException()
        : base($"Zaproszenie o podanych danych nie istnieje.")
    {
    }
}