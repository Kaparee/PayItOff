namespace PayItOff.Shared.Responses;

public class AuditLogResponse
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorAvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Zserializowane dane starych i nowych wartości (opcjonalnie do wyświetlania detali)
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    // Przyjazny opis (wygenerowany przez serwis na podstawie akcji i typu)
    public string FriendlyDescription { get; set; } = string.Empty;
}
