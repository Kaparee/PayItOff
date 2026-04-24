namespace PayItOff.Shared.Requests
{
    public class CreateExpenseBatchRequest
    {
        public required int GroupId { get; set; }
        public required List<SubExpenseDto> SubExpenseDto { get; set; }
    }

    public class SubExpenseDto
    {
        public required int PayerId { get; set; }
        public required string Name { get; set; }
        public required decimal TotalAmount { get; set; }
        public required DateTime PurchasedAt { get; set; }
        public List<ExpenseGroupDto> Groups { get; set; } = [];
        public List<ExpenseItemDto> Items { get; set; } = [];
    }

    public class ExpenseGroupDto
    {
        public required string Name { get; set; }
        public required string Category { get; set; }
        public required decimal TotalAmount { get; set; }
        public required List<int> ParticipantIds { get; set; }
        public required List<ExpenseItemDto> Items { get; set; } = [];
    }

    public class ExpenseItemDto
    {
        public required string Name { get; set; }
        public required string Category { get; set; }
        public required decimal Quantity { get; set; }
        public required decimal UnitPrice { get; set; }
        public required List<int> ParticipantIds { get; set; }
    }
}
