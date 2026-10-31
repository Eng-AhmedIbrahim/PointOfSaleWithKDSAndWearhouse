namespace POS.Services.ComplaintServices;

public class ComplaintService : IComplaintService
{
    private readonly IUnitOfWork _unitOfWork;

    public ComplaintService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ComplaintDto> CreateComplaintAsync(ComplaintDto complaintDto)
    {
        var complaint = new Complaint
        {
            ComplaintNumber = complaintDto.ComplaintNumber,
            CustomerId = complaintDto.CustomerId,
            CustomerName = complaintDto.CustomerName,
            CustomerPhone = complaintDto.CustomerPhone,
            OrderId = complaintDto.OrderId,
            OrderDatabaseId = complaintDto.OrderDatabaseId,
            ComplaintText = complaintDto.ComplaintText,
            ComplaintDate = DateTime.Now,
            Status = ComplaintStatus.Open,
            Note = complaintDto.Note
        };

        await _unitOfWork.Repository<Complaint>().AddAsync(complaint);
        await _unitOfWork.CompleteAsync();

        complaintDto.Id = complaint.Id;
        complaintDto.ComplaintDate = complaint.ComplaintDate;
        complaintDto.Status = complaint.Status.ToString();
        
        return complaintDto;
    }

    public async Task<IEnumerable<ComplaintDto>> GetAllComplaintsAsync()
    {
        var complaints = await _unitOfWork.Repository<Complaint>().GetAllAsync();
        return complaints.Select(c => new ComplaintDto
        {
            Id = c.Id,
            ComplaintNumber = c.ComplaintNumber,
            CustomerId = c.CustomerId,
            CustomerName = c.CustomerName,
            CustomerPhone = c.CustomerPhone,
            OrderId = c.OrderId,
            OrderDatabaseId = c.OrderDatabaseId,
            ComplaintText = c.ComplaintText,
            ComplaintDate = c.ComplaintDate,
            Status = c.Status.ToString(),
            Note = c.Note,
            Resolution = c.Resolution,
            ResolutionDate = c.ResolutionDate
        }).OrderByDescending(c => c.ComplaintDate);
    }

    public async Task<ComplaintDto?> GetComplaintByIdAsync(int id)
    {
        var c = await _unitOfWork.Repository<Complaint>().GetByIdAsync(id);
        if (c == null) return null;

        return new ComplaintDto
        {
            Id = c.Id,
            ComplaintNumber = c.ComplaintNumber,
            CustomerId = c.CustomerId,
            CustomerName = c.CustomerName,
            CustomerPhone = c.CustomerPhone,
            OrderId = c.OrderId,
            OrderDatabaseId = c.OrderDatabaseId,
            ComplaintText = c.ComplaintText,
            ComplaintDate = c.ComplaintDate,
            Status = c.Status.ToString(),
            Note = c.Note,
            Resolution = c.Resolution,
            ResolutionDate = c.ResolutionDate
        };
    }

    public async Task<IEnumerable<ComplaintDto>> GetComplaintsByPhoneAsync(string phone)
    {
        var complaints = await _unitOfWork.Repository<Complaint>()
            .GetAllWithSpecificationAsync(new ComplaintByPhoneSpecification(phone));
        
        return complaints.Select(c => new ComplaintDto
        {
            Id = c.Id,
            ComplaintNumber = c.ComplaintNumber,
            CustomerId = c.CustomerId,
            CustomerName = c.CustomerName,
            CustomerPhone = c.CustomerPhone,
            OrderId = c.OrderId,
            OrderDatabaseId = c.OrderDatabaseId,
            ComplaintText = c.ComplaintText,
            ComplaintDate = c.ComplaintDate,
            Status = c.Status.ToString(),
            Note = c.Note,
            Resolution = c.Resolution,
            ResolutionDate = c.ResolutionDate
        }).OrderByDescending(c => c.ComplaintDate);
    }

    public async Task<bool> UpdateComplaintStatusAsync(int id, string status)
    {
        var complaint = await _unitOfWork.Repository<Complaint>().GetByIdAsync(id);
        if (complaint == null) return false;

        if (Enum.TryParse<ComplaintStatus>(status, true, out var complaintStatus))
        {
            complaint.Status = complaintStatus;
            if (complaintStatus == ComplaintStatus.Resolved || complaintStatus == ComplaintStatus.Closed)
            {
                complaint.ResolutionDate = DateTime.Now;
            }
            _unitOfWork.Repository<Complaint>().Update(complaint);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        return false;
    }
}
