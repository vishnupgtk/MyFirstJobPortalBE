namespace AuthSystemApi.DTOs
{
    public class CompanyProfileDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Industry { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string Locations { get; set; }
        public string CompanyType { get; set; }
        public string EmployerName { get; set; }
    }
}
