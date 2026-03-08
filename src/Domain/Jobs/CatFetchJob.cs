namespace Domain.Jobs;

public class CatFetchJob : BaseEntity
{
    public required CatFetchJobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CatsFetched { get; set; }
}

public enum CatFetchJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}
