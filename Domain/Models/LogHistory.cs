using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Models
{
   public class LogHistory 
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = string.Empty; // "Employee"
        public int EntityId { get; set; } // Employee ID
        public string Action { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE"
        public string? OldValues { get; set; } // JSON string of old values
        public string? NewValues { get; set; } // JSON string of new values
        public string? ChangedFields { get; set; } // Comma-separated list of changed fields
        public string UserId { get; set; } = string.Empty; // Who performed the action
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}

