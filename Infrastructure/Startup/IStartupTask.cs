namespace Infrastructure.Startup
{
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken ct = default);
    }
}
