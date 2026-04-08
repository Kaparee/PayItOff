#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class GroupNotFoundException : PayItOffException
{
    public GroupNotFoundException()
        : base($"Grupa o podanych danych nie istnieje.")
    {
    }
}