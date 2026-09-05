using RagAgents.Core.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagAgents.Api.Services
{
    public interface IChatService
    {
        Task<ChatMessageModel?> AskAsync(QuestionRequest request, ClaimsPrincipal user);
        Task<ChatMessageModel?> AskWithoutAuthAsync(QuestionRequest request);
        Task<ChatMessageModel?> AskWithHistoryAsync(QuestionRequest request, ClaimsPrincipal user);
        Task<IEnumerable<ChatMessageModel>> GetHistoryAsync(string conversationId, ClaimsPrincipal user);
    }
}
