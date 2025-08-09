using System;
using Domain.Enums;
using AutoMapper;
namespace Application.DTOs
{
	public class FilterLogHistoryDto 
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
}

