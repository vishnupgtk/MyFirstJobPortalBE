namespace AuthSystemApi.DTOs
{
    public class JobApplicantDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Skills { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AppliedAt { get; set; }
    }
}
