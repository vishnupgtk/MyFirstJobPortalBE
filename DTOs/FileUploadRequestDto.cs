using Microsoft.AspNetCore.Http;

namespace AuthSystemApi.DTOs
{
    public class FileUploadRequestDto
    {
        public IFormFile File { get; set; } = default!;
    }
}
