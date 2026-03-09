namespace AuthSystemApi.DTOs
{
    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveEmployers { get; set; }
        public int JobSeekers { get; set; }
        public int ActiveJobs { get; set; }
        public decimal UsersChangePercent { get; set; }
        public decimal EmployersChangePercent { get; set; }
        public decimal JobSeekersChangePercent { get; set; }
        public decimal JobsChangePercent { get; set; }
    }
}
