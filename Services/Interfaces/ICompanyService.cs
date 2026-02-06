using AuthSystemApi.DTOs;

public interface ICompanyService
{
    CompanyProfileDto GetProfile(int userId);

    Task RequestProfileChange(int companyId, string fieldName, string newValue, int userId);

    Task<List<PendingChangeRequestDto>> GetPendingRequests();

    Task ApproveChange(int requestId, int adminId);

    Task RejectChange(int requestId, int adminId);

    Task<List<CompanyChangeHistoryDto>> GetCompanyHistory(int companyId);

    Task<List<CompanyChangeHistoryDto>> GetAllHistory();   
}
