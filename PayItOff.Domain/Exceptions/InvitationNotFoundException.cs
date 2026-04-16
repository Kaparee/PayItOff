#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class InvitationNotFoundException : PayItOffException
{
    public InvitationNotFoundException()
        : base("Podane id zaproszenia nie istnieje!")
    {
    }
}