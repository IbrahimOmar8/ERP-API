using System;
using Application.DTOs;
using Domain.Models;

namespace Application.Inerfaces
{
    public interface ILogHistoryService
    {
        Task<IEnumerable<LogHistory>> GetLogsByEntityAsync(string entityName, int entityId);
        Task<IEnumerable<LogHistory>> GetLogsByUserAsync(string userId);
        Task<IEnumerable<LogHistory>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<LogHistory>> GetLogsByActionAsync(string action);

        Task<IEnumerable<LogHistory>> GetAllAsync();
        Task<LogHistory> LogActionAsync(LogHistory logEntry);
        

        Task LogCreateAsync<T>(T entity, string userId, string userName) where T : class;
        Task LogUpdateAsync<T>(T oldEntity, T newEntity, string userId, string userName) where T : class;
        Task LogDeleteAsync<T>(T entity, string userId, string userName) where T : class;

    }
}

