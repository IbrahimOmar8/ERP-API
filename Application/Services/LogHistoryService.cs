using System;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.DTOs;
using Domain.Models;
using Application.Inerfaces;
using System.Text.Json;
using System.Reflection;


namespace Application.Services
{
    public class LogHistoryService : ILogHistoryService
    {
        private readonly ApplicationDbContext _context;

        public LogHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LogHistory>> GetLogsByEntityAsync(string entityName, int entityId)
        {
            return await _context.LogHistories
                .Where(l => l.EntityName == entityName && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<LogHistory>> GetLogsByUserAsync(string userId)
        {
            return await _context.LogHistories
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<LogHistory>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.LogHistories
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<LogHistory>> GetLogsByActionAsync(string action)
        {
            return await _context.LogHistories
                .Where(l => l.Action == action)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<LogHistory> LogActionAsync(LogHistory logEntry)
        {
            await _context.LogHistories.AddAsync(logEntry);
            await _context.SaveChangesAsync();
            return logEntry;
        }

        public async Task<IEnumerable<LogHistory>> GetAllAsync()
        {
            return await _context.LogHistories
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }



        public async Task LogCreateAsync<T>(T entity, string userId, string userName) where T : class
        {
            var entityName = typeof(T).Name;
            var entityId = GetEntityId(entity);

            var logEntry = new LogHistory
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = "CREATE",
                NewValues = JsonSerializer.Serialize(entity, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Notes = $"New {entityName} created"
            };

            await LogActionAsync(logEntry);
        }

        public async Task LogUpdateAsync<T>(T oldEntity, T newEntity, string userId, string userName) where T : class
        {
            var entityName = typeof(T).Name;
            var entityId = GetEntityId(newEntity);
            var changedFields = GetChangedFields(oldEntity, newEntity);

            if (!changedFields.Any())
                return; // No changes detected

            var logEntry = new LogHistory
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = "UPDATE",
                OldValues = JsonSerializer.Serialize(oldEntity, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                NewValues = JsonSerializer.Serialize(newEntity, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                ChangedFields = string.Join(", ", changedFields),
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Notes = $"{entityName} updated - Fields changed: {string.Join(", ", changedFields)}"
            };

            await LogActionAsync(logEntry);
        }

        public async Task LogDeleteAsync<T>(T entity, string userId, string userName) where T : class
        {
            var entityName = typeof(T).Name;
            var entityId = GetEntityId(entity);

            var logEntry = new LogHistory
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = "DELETE",
                OldValues = JsonSerializer.Serialize(entity, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Notes = $"{entityName} deleted"
            };

            await LogActionAsync(logEntry);
        }

        private int GetEntityId<T>(T entity) where T : class
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                return (int)(idProperty.GetValue(entity) ?? 0);
            }
            return 0;
        }

        private List<string> GetChangedFields<T>(T oldEntity, T newEntity) where T : class
        {
            var changedFields = new List<string>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.Name == "Id" || property.Name.Contains("Created") || property.Name.Contains("LastModified"))
                    continue;

                var oldValue = property.GetValue(oldEntity);
                var newValue = property.GetValue(newEntity);

                if (!Equals(oldValue, newValue))
                {
                    changedFields.Add(property.Name);
                }
            }

            return changedFields;
        }
        
    }
}

