using Application.DTOs.Egypt;
using Application.Inerfaces.Egypt;
using Domain.Models.Egypt;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Egypt
{
    public class CompanyProfileService : ICompanyProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEtaTokenService _tokens;

        public CompanyProfileService(ApplicationDbContext context, IEtaTokenService tokens)
        {
            _context = context;
            _tokens = tokens;
        }

        public async Task<CompanyProfileDto?> GetAsync(CancellationToken ct = default)
        {
            var c = await _context.CompanyProfiles.FirstOrDefaultAsync(ct);
            return c == null ? null : Map(c);
        }

        public async Task<CompanyProfileDto> UpsertAsync(UpdateCompanyProfileDto dto, CancellationToken ct = default)
        {
            var c = await _context.CompanyProfiles.FirstOrDefaultAsync(ct);
            if (c == null)
            {
                c = new CompanyProfile();
                _context.CompanyProfiles.Add(c);
            }

            c.NameAr = dto.NameAr;
            c.NameEn = dto.NameEn;
            c.TaxRegistrationNumber = dto.TaxRegistrationNumber;
            c.CommercialRegister = dto.CommercialRegister;
            c.ActivityCode = dto.ActivityCode;
            c.Address = dto.Address;
            c.Governorate = dto.Governorate;
            c.City = dto.City;
            c.Phone = dto.Phone;
            c.Email = dto.Email;
            c.EtaClientId = dto.EtaClientId;
            c.EtaIssuerId = dto.EtaIssuerId;
            c.EtaEnabled = dto.EtaEnabled;

            if (!string.IsNullOrWhiteSpace(dto.EtaClientSecret))
            {
                c.EtaClientSecret = dto.EtaClientSecret;
                if (!string.IsNullOrWhiteSpace(c.EtaClientId))
                    _tokens.Invalidate(c.EtaClientId);
            }

            await _context.SaveChangesAsync(ct);
            return Map(c);
        }

        private static CompanyProfileDto Map(CompanyProfile c) => new()
        {
            Id = c.Id,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            TaxRegistrationNumber = c.TaxRegistrationNumber,
            CommercialRegister = c.CommercialRegister,
            ActivityCode = c.ActivityCode,
            Address = c.Address,
            Governorate = c.Governorate,
            City = c.City,
            Phone = c.Phone,
            Email = c.Email,
            EtaClientId = c.EtaClientId,
            HasEtaSecret = !string.IsNullOrEmpty(c.EtaClientSecret),
            EtaIssuerId = c.EtaIssuerId,
            EtaEnabled = c.EtaEnabled
        };
    }
}
