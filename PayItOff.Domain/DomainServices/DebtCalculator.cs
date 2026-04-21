namespace PayItOff.Domain.DomainServices
{
    public record CalculatedSplit(int UserId, decimal Amount);
    public record CalculatedDebt(int DebtorId, int CreditorId, decimal Amount);
    public record ExpenseCalculationResult(List<CalculatedSplit> Splits, List<CalculatedDebt> Debts);

    public static class DebtCalculator
    {
        public static ExpenseCalculationResult CalculateEqualSplit(
            decimal totalAmount,
            int creditorId,
            List<int> participantIds,
            int? remainderRecipientId = null)
        {
            var splits = new List<CalculatedSplit>();
            var debts = new List<CalculatedDebt>();

            if (participantIds == null || !participantIds.Any() || totalAmount <= 0)
                return new ExpenseCalculationResult(splits, debts);

            int participantsCount = participantIds.Count;

            decimal baseAmount = Math.Floor((totalAmount / participantsCount) * 100) / 100;
            decimal allocatedTotal = baseAmount * participantsCount;
            decimal remainder = totalAmount - allocatedTotal;

            int penniesToDistribute = (int)Math.Round(remainder * 100);

            var sortedParticipants = participantIds.OrderBy(id => id).ToList();

            if (remainderRecipientId.HasValue && sortedParticipants.Contains(remainderRecipientId.Value))
            {
                sortedParticipants.Remove(remainderRecipientId.Value);
                sortedParticipants.Insert(0, remainderRecipientId.Value);
            }

            for (int i = 0; i < participantsCount; i++)
            {
                int currentUserId = sortedParticipants[i];
                decimal amountForPerson = baseAmount;

                if (penniesToDistribute > 0)
                {
                    amountForPerson += 0.01m;
                    penniesToDistribute--;
                }

                if (amountForPerson > 0)
                {
                    splits.Add(new CalculatedSplit(currentUserId, amountForPerson));
                    if (currentUserId != creditorId)
                    {
                        debts.Add(new CalculatedDebt(currentUserId, creditorId, amountForPerson));
                    }
                }
            }

            return new ExpenseCalculationResult(splits, debts);
        }
    }
}