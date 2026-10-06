using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> GetIdMainInventoryAsync()
        {
            var i = await _unitOfWork.InventoryRepository.GetMainInventoryAsync();
            return i.Id;
        }
    }
}
