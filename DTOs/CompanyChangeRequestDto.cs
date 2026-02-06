namespace AuthSystemApi.DTOs
{
    public class CompanyChangeRequestDto
    {
        public int CompanyId { get; set; }
        public string FieldName { get; set; } = "";
        public string NewValue { get; set; } = "";
    }

}
