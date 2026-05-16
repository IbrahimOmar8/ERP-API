using Application.DTOs.HR;
using Domain.Enums;

namespace Application.Inerfaces.HR
{
    public interface IPositionService
    {
        Task<List<PositionDto>> GetAllAsync(CancellationToken ct = default);
        Task<PositionDto> CreateAsync(CreatePositionDto dto, CancellationToken ct = default);
        Task<PositionDto?> UpdateAsync(Guid id, CreatePositionDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IEmployeeHrService
    {
        Task<List<EmployeeFullDto>> GetAllAsync(EmpStatus? status, CancellationToken ct = default);
        Task<EmployeeFullDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<EmployeeFullDto> CreateAsync(CreateEmployeeFullDto dto, CancellationToken ct = default);
        Task<EmployeeFullDto?> UpdateAsync(Guid id, CreateEmployeeFullDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IShiftService
    {
        Task<List<ShiftDto>> GetAllAsync(CancellationToken ct = default);
        Task<ShiftDto> CreateAsync(CreateShiftDto dto, CancellationToken ct = default);
        Task<ShiftDto?> UpdateAsync(Guid id, CreateShiftDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<List<ShiftAssignmentDto>> GetAssignmentsAsync(Guid? employeeId, CancellationToken ct = default);
        Task<ShiftAssignmentDto> AssignAsync(CreateShiftAssignmentDto dto, CancellationToken ct = default);
        Task<bool> RemoveAssignmentAsync(Guid id, CancellationToken ct = default);
    }

    public interface IAttendanceService
    {
        // Punches the clock; idempotent for the day
        Task<AttendanceDto> CheckInAsync(CheckInDto dto, CancellationToken ct = default);
        Task<AttendanceDto?> CheckOutAsync(CheckOutDto dto, CancellationToken ct = default);

        // Admin can record/correct attendance without a live check-in
        Task<AttendanceDto> UpsertManualAsync(ManualAttendanceDto dto, CancellationToken ct = default);

        Task<List<AttendanceDto>> GetAsync(AttendanceFilterDto filter, CancellationToken ct = default);
        Task<List<AttendanceSummaryDto>> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface ILeaveRequestService
    {
        Task<List<LeaveRequestDto>> GetAllAsync(Guid? employeeId, LeaveStatus? status, CancellationToken ct = default);
        Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto dto, CancellationToken ct = default);
        Task<LeaveRequestDto?> SetStatusAsync(Guid id, LeaveStatus status, Guid? approvedByUserId, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IPayrollService
    {
        Task<List<PayrollDto>> GetForPeriodAsync(int year, int month, CancellationToken ct = default);
        Task<PayrollDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<PayrollDto>> GenerateAsync(GeneratePayrollDto dto, CancellationToken ct = default);
        Task<PayrollDto?> SetStatusAsync(Guid id, PayrollStatus status, Guid? userId, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IEmployeeLoanService
    {
        Task<List<EmployeeLoanDto>> GetAllAsync(Guid? employeeId, EmployeeLoanStatus? status, CancellationToken ct = default);
        Task<EmployeeLoanDto> CreateAsync(CreateEmployeeLoanDto dto, Guid? userId, CancellationToken ct = default);
        Task<EmployeeLoanDto?> CancelAsync(Guid id, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
