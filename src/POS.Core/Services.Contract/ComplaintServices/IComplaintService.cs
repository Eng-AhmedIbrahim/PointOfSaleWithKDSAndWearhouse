using POS.Contract.Dtos;

namespace POS.Core.Services.Contract.ComplaintServices;

public interface IComplaintService
{
    Task<ComplaintDto> CreateComplaintAsync(ComplaintDto complaintDto);
    Task<IEnumerable<ComplaintDto>> GetAllComplaintsAsync();
    Task<ComplaintDto?> GetComplaintByIdAsync(int id);
    Task<IEnumerable<ComplaintDto>> GetComplaintsByPhoneAsync(string phone);
    Task<bool> UpdateComplaintStatusAsync(int id, string status);
}
