using AuthSystemApi.Models;

namespace AuthSystemApi.DTOs
{
    public class PaginatedUsersDto
    {
        public List<User> Users { get; set; } = new List<User>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}