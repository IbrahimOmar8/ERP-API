using Application.DTOs;
using Application.Inerfaces;
using AutoMapper;
using MediatR;

namespace Application.Features.LogHistory.Queries.GetLogHistory
{
    public class GetLogHistoryQuery : IRequest<IEnumerable<LogHistoryDto>>
    {
        public string? EntityName { get; set; }
        public int? EntityId { get; set; }
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetLogHistoryQueryHandler : IRequestHandler<GetLogHistoryQuery, IEnumerable<LogHistoryDto>>
    {
        private readonly ILogHistoryService _logHistoryRepository;
        private readonly IMapper _mapper;

        public GetLogHistoryQueryHandler(ILogHistoryService logHistoryRepository, IMapper mapper)
        {
            _logHistoryRepository = logHistoryRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LogHistoryDto>> Handle(GetLogHistoryQuery request, CancellationToken cancellationToken)
        {
            var logs = await GetFilteredLogs(request);
            
            // Apply pagination
            var paginatedLogs = logs
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            return _mapper.Map<IEnumerable<LogHistoryDto>>(paginatedLogs);
        }

        private async Task<IEnumerable<Domain.Models.LogHistory>> GetFilteredLogs(GetLogHistoryQuery request)
        {
            // If specific entity and ID are requested
            if (!string.IsNullOrEmpty(request.EntityName) && request.EntityId.HasValue)
            {
                return await _logHistoryRepository.GetLogsByEntityAsync(request.EntityName, request.EntityId.Value);
            }

            // If specific user is requested
            if (!string.IsNullOrEmpty(request.UserId))
            {
                return await _logHistoryRepository.GetLogsByUserAsync(request.UserId);
            }

            // If specific action is requested
            if (!string.IsNullOrEmpty(request.Action))
            {
                return await _logHistoryRepository.GetLogsByActionAsync(request.Action);
            }

            // If date range is requested
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                return await _logHistoryRepository.GetLogsByDateRangeAsync(request.StartDate.Value, request.EndDate.Value);
            }

            // Default: Get all logs
            return await _logHistoryRepository.GetAllAsync();
        }
    }
}