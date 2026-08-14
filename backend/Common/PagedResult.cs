namespace AssignmentBackend.Common
{
    /// <summary>Generic paginated response envelope.</summary>
    public record PagedResult<T>(
        IEnumerable<T> Items,
        int Page,
        int PageSize,
        int TotalCount)
    {
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
