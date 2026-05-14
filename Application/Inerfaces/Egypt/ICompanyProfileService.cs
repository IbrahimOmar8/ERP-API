using Application.DTOs.Egypt;

namespace Application.Inerfaces.Egypt
{
    public interface ICompanyProfileService
    {
        Task<CompanyProfileDto?> GetAsync(CancellationToken ct = default);
        Task<CompanyProfileDto> UpsertAsync(UpdateCompanyProfileDto dto, CancellationToken ct = default);
    }
}
