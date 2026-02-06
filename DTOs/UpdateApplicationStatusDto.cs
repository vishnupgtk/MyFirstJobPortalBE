namespace AuthSystemApi.DTOs
{
    public class UpdateApplicationStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}