namespace ParkingSystem.Repositories;

public interface IParkingTransaction : IAsyncDisposable
{
    Task CommitAsync();
}