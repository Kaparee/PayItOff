#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class GroupMemberNotFoundException : PayItOffException
{
    public GroupMemberNotFoundException()
        : base($"Użytkownik o podanych danych nie istnieje w tej grupie.")
    {
    }
}