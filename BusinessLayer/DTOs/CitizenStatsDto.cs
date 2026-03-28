namespace BusinessLayer.DTOs
{
    public class CitizenStatsDto
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ResolvedRequests { get; set; }
    }
}