#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class NotificationNotFoundException : PayItOffException
{
    public NotificationNotFoundException()
        : base("Podane id powiadomienia nie istnieje!")
    {
    }
}