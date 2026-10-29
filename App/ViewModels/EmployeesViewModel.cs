using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Services.Interfaces.service;

namespace App.ViewModels
{
    public sealed class EmployeeRow
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = "";
        public bool IsActive { get; init; }
        public bool IsAdmin { get; init; }
        public string StatusText => IsActive ? "Активен" : "Неактивен";

        public static EmployeeRow From(Employee e) => new()
        {
            Id = e.Id,
            FullName = string.Join(" ", new[] { e.LastName, e.FirstName, e.MiddleName }
                .Where(s => !string.IsNullOrWhiteSpace(s))),
            IsActive = e.IsActive,
            IsAdmin = e.IsAdmin
        };
    }

    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly IEmployeeService _service;
        private Guid? _editingId;

        public ObservableCollection<EmployeeRow> Employees { get; } = new();

        [ObservableProperty] private string firstName = "";
        [ObservableProperty] private string middleName = "";
        [ObservableProperty] private string lastName = "";
        [ObservableProperty] private string login = "";
        [ObservableProperty] private string newPassword = "";
        [ObservableProperty] private bool isAdmin;
        [ObservableProperty] private bool isActive = true;
        [ObservableProperty] private bool isEditing;
        [ObservableProperty] private bool isFormOpen;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private string listErrorMessage = "";
        [ObservableProperty] private string successMessage = "";

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool HasListError => !string.IsNullOrWhiteSpace(ListErrorMessage);
        public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
        public string FormTitle => IsEditing ? "Изменение сотрудника" : "Новый сотрудник";
        public string FormSubtitle => IsEditing
            ? "Измените нужные данные и сохраните результат"
            : "Заполните данные нового сотрудника";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Добавить сотрудника";

        public EmployeesViewModel(IEmployeeService service) => _service = service;

        partial void OnIsEditingChanged(bool value)
        {
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(FormSubtitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
        partial void OnListErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasListError));
        partial void OnSuccessMessageChanged(string value) => OnPropertyChanged(nameof(HasSuccess));

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ListErrorMessage = "";
            try
            {
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                ListErrorMessage = "Не удалось загрузить сотрудников: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            var all = await _service.GetAllAsync();
            Employees.Clear();
            foreach (var employee in all)
                Employees.Add(EmployeeRow.From(employee));
        }

        private void ClearForm()
        {
            _editingId = null;
            FirstName = "";
            MiddleName = "";
            LastName = "";
            Login = "";
            NewPassword = "";
            IsAdmin = false;
            IsActive = true;
            IsEditing = false;
            ErrorMessage = "";
        }

        [RelayCommand]
        private void OpenCreate()
        {
            if (IsBusy) return;
            ClearForm();
            ListErrorMessage = "";
            SuccessMessage = "";
            IsFormOpen = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            if (IsBusy) return;
            IsFormOpen = false;
            ClearForm();
        }

        [RelayCommand]
        private void DismissNotice() => SuccessMessage = "";

        [RelayCommand]
        private async Task EditEmployeeAsync(EmployeeRow? row)
        {
            if (row is null || IsBusy) return;
            IsBusy = true;
            ListErrorMessage = "";
            SuccessMessage = "";
            try
            {
                var employee = await _service.GetByIdAsync(row.Id)
                    ?? throw new InvalidOperationException("Сотрудник не найден.");

                ClearForm();
                _editingId = employee.Id;
                FirstName = employee.FirstName;
                MiddleName = employee.MiddleName;
                LastName = employee.LastName;
                IsActive = employee.IsActive;
                IsAdmin = employee.IsAdmin;
                Login = employee.Login ?? "";
                NewPassword = ""; // Пароль и его хеш никогда не показываем в форме.
                IsEditing = true;
                IsFormOpen = true;
            }
            catch (Exception ex)
            {
                ListErrorMessage = "Не удалось открыть сотрудника: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsBusy || !IsFormOpen) return;
            IsBusy = true;
            ErrorMessage = "";

            try
            {
                bool wasEditing = IsEditing;
                var saved = await _service.SaveAsync(
                    _editingId, FirstName, MiddleName, LastName,
                    IsActive, IsAdmin, Login, NewPassword);

                string fullName = string.Join(" ", new[]
                {
                    saved.LastName, saved.FirstName, saved.MiddleName
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                // Мгновенно отражаем сохранённый статус в списке.
                // Для нового сотрудника добавляем строку, для существующего заменяем.
                var updatedRow = EmployeeRow.From(saved);
                int rowIndex = -1;
                for (int i = 0; i < Employees.Count; i++)
                {
                    if (Employees[i].Id == saved.Id)
                    {
                        rowIndex = i;
                        break;
                    }
                }

                if (rowIndex >= 0)
                    Employees[rowIndex] = updatedRow;
                else
                    Employees.Add(updatedRow);

                // Сохранение завершено: закрываем окно и подтверждаем результат.
                IsFormOpen = false;
                ClearForm();
                SuccessMessage = wasEditing
                    ? $"Изменения сотрудника «{fullName}» сохранены. Статус: {(saved.IsActive ? "Активен" : "Неактивен")}."
                    : $"Сотрудник «{fullName}» успешно добавлен.";

                try
                {
                    await RefreshAsync();
                    ListErrorMessage = "";
                }
                catch (Exception ex)
                {
                    // Сохранение уже прошло успешно. Ошибка обновления списка
                    // не должна выдаваться за ошибку создания сотрудника.
                    ListErrorMessage = "Сохранено, но не удалось обновить список: " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                // Оставляем форму и введённые данные, чтобы исправить ошибку.
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
