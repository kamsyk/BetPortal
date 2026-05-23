using Microsoft.AspNetCore.Http;

namespace BetPortalWebApi.Utils
{
    public static class ListQueryProcessor
    {
        public static IQueryable<TEntity> Apply<TEntity>(
            IQueryable<TEntity> query,
            Contracts.List.ListQueryRequest request,
            IQueryCollection queryParams,
            IReadOnlyDictionary<string, Func<IQueryable<TEntity>, string, IQueryable<TEntity>>> filterHandlers,
            IReadOnlyDictionary<string, Func<IQueryable<TEntity>, bool, IQueryable<TEntity>>> sortHandlers,
            string defaultSortKey)
        {
            foreach (var filter in filterHandlers)
            {
                if (!queryParams.TryGetValue(filter.Key, out var values))
                {
                    continue;
                }

                var value = values.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                query = filter.Value(query, value);
            }

            var requestedSort = request.SortBy?.Trim();
            var sortKey = string.IsNullOrWhiteSpace(requestedSort) ? defaultSortKey : requestedSort;
            var isDescending = string.Equals(request.SortOrder?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

            if (!sortHandlers.TryGetValue(sortKey, out var sortHandler))
            {
                sortHandler = sortHandlers[defaultSortKey];
            }

            return sortHandler(query, isDescending);
        }
    }
}
