namespace POS.Services.DistributionServices;

public class DistributionServices : IDistributionServices
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DistributionServices(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ICollection<Orders>> GetUnCompletedDeliveryOrdersAsync()
    {
        var spec = new BaseSpecifications<Orders>(o => 
            o.OrderType == OrderTypes.Delivery &&
            o.OrderState != OrderStates.Completed &&
            o.OrderState != OrderStates.Voided &&
            o.OrderState != OrderStates.Canceled
        );
        spec.Includes.Add(o => o.OrderDetails!);
        
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.ToList();
    }

    public async Task<bool> UpdateDeliveryOrdersAfterDispatchAsync(OrderDto orderDto)
    {
        if (orderDto == null) return false;

        var spec = new BaseSpecifications<Orders>(o => o.Id == orderDto.Id && (orderDto.BranchId == 0 || o.BranchID == orderDto.BranchId));
        var order = (await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec)).FirstOrDefault();
        
        if (order == null) return false;

        order.OrderState = OrderStates.Dispatched;
        order.AssignTime = orderDto.AssignTime ?? DateTime.Now;
        
        if (!string.IsNullOrEmpty(orderDto.DriverID))
        {
            order.DriverID = orderDto.DriverID;
            order.DriverName = orderDto.DriverName;
        }

        order.DispatchID = orderDto.DispatchID;

        // Fetch Delivery Bonus from Zone and ensure Zone info is present
        var zoneId = order.ZoneID ?? orderDto.ZoneID;
        if (zoneId.HasValue)
        {
            var zone = await _unitOfWork.Repository<DeliveryZone>().GetByIdAsync(zoneId.Value);
            if (zone != null)
            {
                order.ZoneID = zoneId;
                if (string.IsNullOrEmpty(order.ZoneName)) order.ZoneName = zone.ZoneName;
                order.ZoneBonus = zone.ZoneBonus;
            }
        }

        _unitOfWork.Repository<Orders>().Update(order);
        return await _unitOfWork.CompleteAsync() > 0;
    }
    
    public async Task<bool> UnDispatchOrderAsync(int id)
    {
        var spec = new BaseSpecifications<Orders>(o => o.Id == id);
        var order = (await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec)).FirstOrDefault();
        
        if (order == null) return false;

        // Reset driver info
        order.DriverID = null;
        order.DriverName = null;
        order.AssignTime = null;
        order.DispatchID = null;

        // Set back to Assigned ("تم التعيين" في الفرع بانتظار تعيين طيار)
        order.OrderState = OrderStates.Assigned;

        _unitOfWork.Repository<Orders>().Update(order);
        return await _unitOfWork.CompleteAsync() > 0;
    }


    public async Task<ICollection<Orders>> GetOrdersByDriverAsync(string driverId)
    {
        var spec = new BaseSpecifications<Orders>(o => 
            o.OrderType == OrderTypes.Delivery &&
            o.DriverID == driverId &&
            o.OrderState != OrderStates.Completed &&
            o.OrderState != OrderStates.Voided &&
            o.OrderState != OrderStates.Canceled
        );
        
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.ToList();
    }

    public async Task<List<DriverSettlementDto>> GetDriverSettlementAsync(DateTime posDate)
    {
        var spec = new BaseSpecifications<Orders>(o => 
            o.OrderType == OrderTypes.Delivery &&
            o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date &&
            o.OrderState != OrderStates.Voided &&
            o.OrderState != OrderStates.Canceled
        );

        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        
        return orders
            .Where(o => !string.IsNullOrEmpty(o.DriverID))
            .GroupBy(o => new { o.DriverID, o.DriverName })
            .Select(g => new DriverSettlementDto
            {
                DriverId = g.Key.DriverID!,
                DriverName = g.Key.DriverName ?? "Unknown",
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.GrandTotal ?? 0),
                TotalBonus = g.Sum(o => o.ZoneBonus ?? 0),
                Orders = _mapper.Map<List<OrderDto>>(g.ToList())
            })
            .ToList();
    }


    public async Task<ICollection<Orders>> GetVoidedDeliveryOrdersAsync(DateTime posDate)
    {
        var spec = new BaseSpecifications<Orders>(o => 
            o.OrderType == OrderTypes.Delivery &&
            o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date &&
            (o.OrderState == OrderStates.Voided || (o.OrderDetails != null && o.OrderDetails.Any(d => d.IsVoided == true)))
        );
        spec.Includes.Add(o => o.OrderDetails!);
        
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.ToList();
    }
}
