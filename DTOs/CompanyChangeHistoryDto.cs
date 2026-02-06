namespace AuthSystemApi.DTOs
{
    public class CompanyChangeHistoryDto
    {
        public string CompanyName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string ApprovedBy { get; set; } = "";
        public string RequestedBy { get; set; } = "";
        public DateTime ChangedAt { get; set; }
    }



}
