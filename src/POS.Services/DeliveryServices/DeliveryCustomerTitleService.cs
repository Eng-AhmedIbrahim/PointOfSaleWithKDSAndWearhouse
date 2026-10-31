namespace POS.Services.DeliveryServices;

public class DeliveryCustomerTitleService : IDeliveryCustomerTitleService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryCustomerTitleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DeliveryTitleToReturnDto>> GetAllAsync()
    {
        var titles = await _unitOfWork.Repository<DeliveryCustomerTitle>().GetAllAsync();
        return titles.Select(t => new DeliveryTitleToReturnDto
        {
            Id = t.Id,
            TitleName = t.TitleName
        });
    }

    public async Task<DeliveryTitleToReturnDto?> GetByIdAsync(int id)
    {
        var title = await _unitOfWork.Repository<DeliveryCustomerTitle>().GetByIdAsync(id);
        return title == null ? null : new DeliveryTitleToReturnDto
        {
            Id = title.Id,
            TitleName = title.TitleName
        };
    }

    public async Task AddAsync(DeliveryTitleDto titleDto)
    {
        var title = new DeliveryCustomerTitle
        {
            TitleName = titleDto.TitleName
        };

        await _unitOfWork.Repository<DeliveryCustomerTitle>().AddAsync(title);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateAsync(DeliveryCustomerTitle titleDto)
    {
        var existingTitle = await _unitOfWork.Repository<DeliveryCustomerTitle>().GetByIdAsync(titleDto.Id);
        if (existingTitle != null)
        {
            existingTitle.TitleName = titleDto.TitleName;
            _unitOfWork.Repository<DeliveryCustomerTitle>().Update(existingTitle);
            await _unitOfWork.CompleteAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var title = await _unitOfWork.Repository<DeliveryCustomerTitle>().GetByIdAsync(id);
        if (title != null)
        {
            _unitOfWork.Repository<DeliveryCustomerTitle>().Delete(title);
            await _unitOfWork.CompleteAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
        => await _unitOfWork.Repository<DeliveryCustomerTitle>().GetByIdAsync(id) != null;

}
