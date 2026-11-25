using HealthyFac.Insights.Application.Chats;
using HealthyFac.Shared.Application.Grids.Keyset;
using ApplicationSearchChatsRequest = HealthyFac.Insights.Application.Chats.SearchChatsRequest;

namespace HealthyFac.Insights.Web.Chats;

public record SearchChatsRequest : KeysetGridRequest<ChatGridFilter, ChatSortColumn>;

public static class SearchChatsRequestExtensions
{
    extension(SearchChatsRequest request)
    {
        public ApplicationSearchChatsRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
                After = request.After,
            };
    }
}
