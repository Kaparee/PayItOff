#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class InvalidGroupActionException : PayItOffException
{
    public InvalidGroupActionException()
        : base($"Nie można wykonać tej akcji dla podanej grupy!")
    {
    }
}