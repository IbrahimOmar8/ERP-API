using Application.DTOs;
using AutoMapper;
using Domain.Models;

namespace ERPTask.Application.Mappings
{
    public class EmployeeMappingProfile : Profile
    {
        public EmployeeMappingProfile()
        {
            CreateMap<CreateEmployeeDto, Employee>();
            CreateMap<Employee, EmployeeDto>();
            CreateMap<UpdateEmployeeDto, Employee>();
            CreateMap<Employee, UpdateEmployeeDto>();

        CreateMap<Domain.Models.LogHistory, LogHistoryDto>();
        CreateMap<LogHistoryDto, Domain.Models.LogHistory>();

        }
    }
}