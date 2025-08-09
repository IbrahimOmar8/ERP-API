using Application.DTOs;
using AutoMapper;
using Domain.Models;

namespace ERPTask.Application.Mappings
{
    public class DepartmentMappingProfile : Profile
    {
        public DepartmentMappingProfile()
        {
            CreateMap<CreateDepartmentDto, Department>();
            CreateMap<Department, DepartmentDto>();  

           


        }
    }
}