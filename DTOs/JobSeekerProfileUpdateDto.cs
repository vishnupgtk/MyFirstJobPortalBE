namespace AuthSystemApi.DTOs
{
    public class JobSeekerProfileUpdateDto
    {
        public int UserId { get; set; }  
        public string Summary { get; set; } = "";
        public string Education { get; set; } = "";
        public string College { get; set; } = "";
        public string Skills { get; set; } = "";
    }

}
