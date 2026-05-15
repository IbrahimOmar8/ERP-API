namespace Application.DTOs.Import
{
    public class ImportResultDto
    {
        public int Total { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public bool DryRun { get; set; }
    }

    public class ImportRowError
    {
        public int Row { get; set; }
        public string? Field { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
