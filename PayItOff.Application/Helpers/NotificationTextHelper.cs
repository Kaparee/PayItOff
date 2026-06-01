namespace PayItOff.Application.Helpers;

public static class NotificationTextHelper
{
    public static string ExpenseAdded(string groupName, string expenseName, string creatorFullName, string creditorFullName, decimal amount) =>
        $"W grupie „{groupName}” dodano wydatek „{expenseName}” (autor: {creatorFullName}). Kwota do zapłaty ({creditorFullName}): {amount:N2} zł";
}
