using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.WarehouseKeeper}")]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IWebHostEnvironment env, ILogger<FilesController> logger)
        {
            _env = env;
            _logger = logger;
        }

        private static readonly string[] AllowedImageExt = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
        private const long MaxImageBytes = 5_000_000; // 5 MB

        // multipart/form-data: file=<image>
        [HttpPost("images")]
        [RequestSizeLimit(MaxImageBytes + 100_000)]
        public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0) return BadRequest(new { error = "الملف فارغ" });
            if (file.Length > MaxImageBytes)
                return BadRequest(new { error = "حجم الصورة يتجاوز 5 ميجابايت" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExt.Contains(ext))
                return BadRequest(new { error = "نوع الملف غير مدعوم. المسموح: png, jpg, jpeg, webp, gif" });

            var uploadsRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "images");
            Directory.CreateDirectory(uploadsRoot);

            var filename = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, filename);
            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Public URL — served via UseStaticFiles in Program.cs
            var url = $"/uploads/images/{filename}";
            return Ok(new { url, filename, size = file.Length });
        }

        [HttpDelete("images/{filename}")]
        public IActionResult DeleteImage(string filename)
        {
            // Don't allow path traversal
            if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
                return BadRequest(new { error = "اسم ملف غير صالح" });

            var path = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "images", filename);
            if (!System.IO.File.Exists(path)) return NotFound();
            try
            {
                System.IO.File.Delete(path);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete {File}", path);
                return StatusCode(500);
            }
        }
    }
}
