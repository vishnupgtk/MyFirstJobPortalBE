namespace AuthSystemApi.DTOs
{
    public class PendingChangeRequestDto
    {
        public int RequestId { get; set; }
        public int CompanyId { get; set; }    
        public string CompanyName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string RequestedBy { get; set; } = "";
        public DateTime RequestedAt { get; set; }
    }


}
