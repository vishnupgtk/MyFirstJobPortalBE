using AuthSystemApi.DTOs;

namespace AuthSystemApi.Services.Interfaces
{
    public interface IChatbotService
    {
        Task<ChatResponseDto> ProcessMessageAsync(int userId, string userType, SendChatMessageDto messageDto);
        Task<List<ChatMessageDto>> GetChatHistoryAsync(int userId, string sessionId = null);
        Task SaveChatMessageAsync(int userId, string userType, string message, string response, string sessionId);
    }
}
