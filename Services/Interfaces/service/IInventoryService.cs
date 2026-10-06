namespace Services.Interfaces.service
{
    public interface IInventoryService
    {
        public Task<Guid> GetIdMainInventoryAsync();
    }
}
