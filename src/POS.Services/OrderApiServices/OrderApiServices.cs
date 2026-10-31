using Microsoft.AspNetCore.Hosting;
using POS.Contract.Dtos.OrderDto;
using POS.Contract.Dtos.OrderDtos;
using POS.Core.Services.Contract.CompanyService;
using POS.Core.Services.Contract.OrderApiServices;

namespace POS.Services.OrderApiServices;

public class OrderApiServices : IOrderApiServices
{
    private readonly IBranchService _branchService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public OrderApiServices(IBranchService branchService, IWebHostEnvironment webHostEnvironment)
    {
        _branchService = branchService;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<List<string>> GetBranchDetails(OrderDto orderDto)
    {
        var branch = await _branchService.GetBranchByIdAsync(orderDto.BranchId)!;
        var logoFileName = branch!.ImagePath ?? string.Empty;
        var logoWidth = branch.LogoWidth;
        var logoHeight = branch.LogoHeight;

        string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "Files", "images", logoFileName);

        return new List<string>
        {
            logoPath,
            logoWidth.ToString(),
            logoHeight.ToString()
        };
    }
}
