using System.Diagnostics;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

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

        // Streams a logical mysqldump of the active database.
        // Requires `mysqldump` to be present on the server's PATH.
        [HttpGet("download")]
        public async Task<IActionResult> Download(CancellationToken ct)
        {
            if (!TryParseConnection(out var b, out var error))
                return BadRequest(new { error });

            var tempFile = Path.Combine(Path.GetTempPath(), $"erp-backup-{Guid.NewGuid():N}.sql");
            var psi = new ProcessStartInfo
            {
                FileName = "mysqldump",
                RedirectStandardError = true,
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add($"--host={b.Server}");
            psi.ArgumentList.Add($"--port={b.Port}");
            psi.ArgumentList.Add($"--user={b.UserID}");
            psi.ArgumentList.Add("--single-transaction");
            psi.ArgumentList.Add("--default-character-set=utf8mb4");
            psi.ArgumentList.Add($"--result-file={tempFile}");
            psi.ArgumentList.Add(b.Database);
            // Avoid leaking the password on the command line; mysqldump reads $MYSQL_PWD.
            psi.Environment["MYSQL_PWD"] = b.Password ?? string.Empty;

            try
            {
                using var proc = Process.Start(psi)
                    ?? throw new InvalidOperationException("تعذّر تشغيل mysqldump — تأكد من تثبيته في المسار");
                var stderr = await proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);
                if (proc.ExitCode != 0)
                    return StatusCode(500, new { error = "فشل النسخ الاحتياطي", details = stderr });

                var bytes = await System.IO.File.ReadAllBytesAsync(tempFile, ct);
                var filename = $"erp-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.sql";
                return File(bytes, "application/sql", filename);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return StatusCode(500, new
                {
                    error = "mysqldump غير موجود على الخادم. ثبّت أدوات MySQL أو استخدم نسخاً احتياطياً خارجياً."
                });
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    try { System.IO.File.Delete(tempFile); } catch { }
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> Info(CancellationToken ct)
        {
            if (!TryParseConnection(out var b, out var error))
                return Ok(new { exists = false, error });

            try
            {
                // Aggregate size from INFORMATION_SCHEMA — gives us the data + index pages used.
                var conn = _context.Database.GetDbConnection();
                await conn.OpenAsync(ct);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        IFNULL(SUM(data_length + index_length), 0) AS total_bytes,
                        IFNULL(MAX(update_time), NOW()) AS last_updated
                    FROM information_schema.tables
                    WHERE table_schema = @schema";
                var p = cmd.CreateParameter();
                p.ParameterName = "@schema";
                p.Value = b.Database;
                cmd.Parameters.Add(p);

                using var reader = await cmd.ExecuteReaderAsync(ct);
                if (!await reader.ReadAsync(ct))
                    return Ok(new { exists = false });

                var sizeBytes = reader.IsDBNull(0) ? 0L : Convert.ToInt64(reader.GetValue(0));
                var lastUpdated = reader.IsDBNull(1) ? DateTime.UtcNow : reader.GetDateTime(1);
                return Ok(new
                {
                    exists = true,
                    database = b.Database,
                    sizeBytes,
                    lastModifiedUtc = DateTime.SpecifyKind(lastUpdated, DateTimeKind.Utc),
                });
            }
            catch (Exception ex)
            {
                return Ok(new { exists = false, error = ex.Message });
            }
        }

        private bool TryParseConnection(out MySqlConnectionStringBuilder builder, out string? error)
        {
            builder = new MySqlConnectionStringBuilder();
            var cs = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(cs))
            {
                error = "ConnectionStrings:DefaultConnection غير معرّف";
                return false;
            }
            try
            {
                builder.ConnectionString = cs;
                if (string.IsNullOrEmpty(builder.Database))
                {
                    error = "اسم قاعدة البيانات غير محدد في ConnectionStrings";
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
