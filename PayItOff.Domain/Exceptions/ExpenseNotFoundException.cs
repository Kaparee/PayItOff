#pragma warning disable RCS1194

namespace PayItOff.Domain.Exceptions;

public class ExpenseNotFoundException : PayItOffException
{
    public ExpenseNotFoundException()
        : base("Nie znaleziono wskazanego wydatku.")
    {
    }
}
