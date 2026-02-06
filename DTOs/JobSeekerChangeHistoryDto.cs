namespace AuthSystemApi.DTOs
{
    public class JobSeekerChangeHistoryDto
    {
        public string FieldName { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string ChangedBy { get; set; } = "";
        public DateTime ChangedAt { get; set; }
    }

}
