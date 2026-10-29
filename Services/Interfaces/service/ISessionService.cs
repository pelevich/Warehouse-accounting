using System.ComponentModel;

namespace Services.Interfaces.service
{
    public interface ISessionService : INotifyPropertyChanged
    {
        Guid? CurrentEmployeeId { get; }
        Guid? CurrentShiftId { get; }
        bool IsAdmin { get; }
        bool IsAuthenticated { get; }

        void StartCashier(Guid employeeId, Guid shiftId);
        void StartAdmin(Guid employeeId);
        void SetCurrentShift(Guid? shiftId);
        void SignOut();
    }
}
