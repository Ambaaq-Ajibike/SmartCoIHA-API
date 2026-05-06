namespace Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ID of the user or system component performing the action
        /// Nullable because some actions might be triggered by background background workers
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Example: "Create", "Update", "Delete", or Custom Business Logic like "VerificationStatusUpdate"
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// The name of the Entity/Table being modified (e.g., "Institution", "DataRequest")
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// The primary key of the modified record
        /// </summary>
        public string PrimaryKey { get; set; } = string.Empty;

        /// <summary>
        /// JSON representation of the entity state before the update
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON representation of the entity state after the update
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// When the action occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}