using POS.Contract.Dtos;

namespace BlazorBase.ERPFrontServices.ComplaintServices;

public interface IComplaintServices
{
    Task<ComplaintDto> CreateComplaintAsync(ComplaintDto complaintDto);
    Task<IReadOnlyList<ComplaintDto>> GetAllComplaintsAsync();
    Task<ComplaintDto> GetComplaintByIdAsync(int id);
    Task<IEnumerable<ComplaintDto>> GetComplaintsByPhoneAsync(string phone);
    Task<bool> UpdateComplaintStatusAsync(int id, string status);
}
