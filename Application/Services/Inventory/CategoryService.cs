using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .Include(c => c.ParentCategory)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    NameAr = c.NameAr,
                    NameEn = c.NameEn,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentName = c.ParentCategory != null ? c.ParentCategory.NameAr : null,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return null;

            return new CategoryDto
            {
                Id = category.Id,
                NameAr = category.NameAr,
                NameEn = category.NameEn,
                ParentCategoryId = category.ParentCategoryId,
                ParentName = category.ParentCategory?.NameAr,
                IsActive = category.IsActive
            };
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var category = new Category
            {
                NameAr = dto.NameAr,
                NameEn = dto.NameEn,
                ParentCategoryId = dto.ParentCategoryId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(category.Id))!;
        }

        public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;

            category.NameAr = dto.NameAr;
            category.NameEn = dto.NameEn;
            category.ParentCategoryId = dto.ParentCategoryId;
            category.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
