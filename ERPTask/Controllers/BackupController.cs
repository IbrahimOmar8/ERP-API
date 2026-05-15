using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class BackupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public BackupController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Returns a copy of the SQLite database file as a download.
        // Streamed via .backup so it's safe even with concurrent connections.
        [HttpGet("download")]
        public async Task<IActionResult> Download()
        {
            var dbPath = ResolveDbPath();
            if (string.IsNullOrEmpty(dbPath) || !System.IO.File.Exists(dbPath))
                return NotFound(new { error = "ملف قاعدة البيانات غير موجود" });

            var tempFile = Path.Combine(Path.GetTempPath(), $"erp-backup-{Guid.NewGuid():N}.db");
            try
            {
                // Use SQLite's native backup so we don't corrupt an in-flight write
                var conn = _context.Database.GetDbConnection();
                await conn.OpenAsync();
                using (var src = (Microsoft.Data.Sqlite.SqliteConnection)conn)
                using (var dst = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempFile}"))
                {
                    await dst.OpenAsync();
                    src.BackupDatabase(dst);
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(tempFile);
                var filename = $"erp-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db";
                return File(bytes, "application/octet-stream", filename);
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    try { System.IO.File.Delete(tempFile); } catch { }
            }
        }

        [HttpGet("info")]
        public IActionResult Info()
        {
            var dbPath = ResolveDbPath();
            if (string.IsNullOrEmpty(dbPath) || !System.IO.File.Exists(dbPath))
                return Ok(new { exists = false });

            var fi = new FileInfo(dbPath);
            return Ok(new
            {
                exists = true,
                sizeBytes = fi.Length,
                lastModifiedUtc = fi.LastWriteTimeUtc,
            });
        }

        private string? ResolveDbPath()
        {
            // Pull from "ConnectionStrings:DefaultConnection" — only Sqlite is supported.
            var cs = _config.GetConnectionString("DefaultConnection") ?? "";
            const string marker = "Data Source=";
            var idx = cs.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var path = cs.Substring(idx + marker.Length);
            var semi = path.IndexOf(';');
            if (semi >= 0) path = path.Substring(0, semi);
            return path.Trim();
        }
    }
}
