namespace AuthSystemApi.DTOs
{
    public class JobSeekerProfileViewDto
    {
        public int JobSeekerId { get; set; }
        public int UserId { get; set; }
        public string Summary { get; set; } = "";
        public string Education { get; set; } = "";
        public string College { get; set; } = "";
        public string Skills { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }

}
