using Services.Interfaces.service;
using System.ComponentModel;

namespace Services.service
{
    public sealed class SessionService : ISessionService
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid? CurrentEmployeeId { get; private set; }
        public Guid? CurrentShiftId { get; private set; }
        public bool IsAdmin { get; private set; }
        public bool IsAuthenticated => CurrentEmployeeId.HasValue;

        public void StartCashier(Guid employeeId, Guid shiftId)
        {
            if (employeeId == Guid.Empty || shiftId == Guid.Empty)
                throw new ArgumentException("Некорректные данные смены.");

            CurrentEmployeeId = employeeId;
            CurrentShiftId = shiftId;
            IsAdmin = false;

            Notify();
        }

        // Вызывать исключительно после успешной проверки пароля в IAuthService.
        public void StartAdmin(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
                throw new ArgumentException(
                    "Некорректный идентификатор администратора.");

            CurrentEmployeeId = employeeId;
            CurrentShiftId = null;
            IsAdmin = true;

            Notify();
        }

        public void SetCurrentShift(Guid? shiftId)
        {
            CurrentShiftId = shiftId;

            PropertyChanged?.Invoke(
                this, new(nameof(CurrentShiftId)));
        }

        public void SignOut()
        {
            CurrentEmployeeId = null;
            CurrentShiftId = null;
            IsAdmin = false;

            Notify();
        }

        private void Notify()
        {
            PropertyChanged?.Invoke(this, new(nameof(CurrentEmployeeId)));
            PropertyChanged?.Invoke(this, new(nameof(CurrentShiftId)));
            PropertyChanged?.Invoke(this, new(nameof(IsAdmin)));
            PropertyChanged?.Invoke(this, new(nameof(IsAuthenticated)));
        }
    }
}
