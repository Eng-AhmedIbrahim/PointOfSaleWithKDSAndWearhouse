namespace POS.API.Controllers.DeliveryControllers;

public class DeliveryCustomerController : BaseApiController
{
    private readonly IDeliveryCustomerService _deliveryCustomerService;
    private readonly IMapper _mapper;

    private readonly IOrderService _orderService;

    public DeliveryCustomerController(IDeliveryCustomerService deliveryCustomerService, IMapper mapper, IOrderService orderService)
    {
        _deliveryCustomerService = deliveryCustomerService;
        _mapper = mapper;
        _orderService = orderService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var customer = await _deliveryCustomerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound("Customer not found");

        var customerDto = _mapper.Map<DeliveryCustomerToReturnDto>(customer); 
        return Ok(customerDto);
    }


    [HttpGet("with-addresses/{id}")]
    public async Task<IActionResult> GetCustomerWithAddresses(int id)
    {
        var customer = await _deliveryCustomerService.GetCustomerWithAddressesAsync(id);
        if (customer == null)
            return NotFound("Customer not found");

        var customerDto = _mapper.Map<DeliveryCustomerToReturnDto>(customer);
        return Ok(customerDto);
    }


    [HttpGet("Get-Customer-By-Phone/{phoneNumber}")]
    public async Task<IActionResult> GetCustomerByPhoneNumber(string phoneNumber)
    {
        var customer = await _deliveryCustomerService.GetCustomerByPhoneNumberAsync(phoneNumber);
        if (customer == null)
            return NotFound("Customer not found");

        var customerDto = _mapper.Map<DeliveryCustomerToReturnDto>(customer);

        // Fetch orders - already ordered DESC by OrderDate in specification
        var orders = await _orderService.GetOrdersByCustomerPhoneAsync(phoneNumber);
        
        if (orders != null && orders.Any())
        {
            // Calculate stats efficiently
            customerDto.OrderCount = orders.Count;
            customerDto.TotalOrdersAmount = orders.Sum(o => o.GrandTotal ?? 0);
            customerDto.AverageOrderValue = customerDto.OrderCount > 0 
                ? Math.Round(customerDto.TotalOrdersAmount / customerDto.OrderCount, 2) 
                : 0;
            
            // Orders are already sorted DESC, so first = latest, last = oldest
            customerDto.LastOrderDate = orders.First().OrderDate;
            var lastOrder = orders.First();
            // TakerName = the cashier/taker who took the last order
            customerDto.LastReceiverName = !string.IsNullOrWhiteSpace(lastOrder?.TakerName)
                ? lastOrder!.TakerName
                : (lastOrder?.CashierName ?? lastOrder?.CustomerName ?? lastOrder?.TakeawayCustomerName);
            
            customerDto.FirstOrderDate = orders.Last().OrderDate;

            // Take first 10 (already sorted DESC, so these are the latest 10)
            // Map WITHOUT OrderDetails for performance - we don't need item details in the list view
            var last10 = orders.Take(10).Select(o => new POS.Contract.Dtos.OrderDtos.OrderDto
            {
                OrderId = o.OrderID,
                OrderDate = o.OrderDate,
                GrandTotal = o.GrandTotal,
                OrderState = o.OrderState.ToString(),
                CustomerName = o.CustomerName,
                CustomerPhone = o.Phone1,
                BranchId = o.BranchID,
                PaymentMethod = o.PaymentMethod
                // Intentionally NOT mapping OrderDetails here for performance
            }).ToList();
            
            customerDto.Last10Orders = last10;

            // Check for multiple active orders
            var activeOrderSummaries = orders.Where(o =>
                o.OrderState != OrderStates.Completed &&
                o.OrderState != OrderStates.Voided &&
                o.OrderState != OrderStates.FailedToDeliverToBranch &&
                o.OrderState != OrderStates.Canceled).ToList();

            if (activeOrderSummaries.Any())
            {
                customerDto.ActiveOrders = new List<POS.Contract.Dtos.OrderDtos.OrderDto>();
                foreach (var activeSummary in activeOrderSummaries)
                {
                    // Fetch full order details separately since summary doesn't have details
                    var fullActiveOrder = await _orderService.GetOrderByIdAsync(activeSummary.Id);
                    if (fullActiveOrder != null)
                    {
                        customerDto.ActiveOrders.Add(_mapper.Map<POS.Contract.Dtos.OrderDtos.OrderDto>(fullActiveOrder));
                    }
                }
            }
        }

        return Ok(customerDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await _deliveryCustomerService.GetAllCustomersAsync();
        var customersDto = _mapper.Map<IEnumerable<DeliveryCustomerToReturnDto>>(customers);
        return Ok(customersDto);
    }

    [HttpPost("create-customer")]
    public async Task<IActionResult> CreateCustomer(DeliveryCustomerDto customerDto)
    {
        var customer = _mapper.Map<DeliveryCustomerDto, DeliveryCustomerInfo>(customerDto);

        var createdCustomer = await _deliveryCustomerService.CreateCustomerAsync(customer);

        if (createdCustomer is null)
            return BadRequest("Failed to create customer.");

        var customerToReturn = _mapper.Map<DeliveryCustomerToReturnDto>(createdCustomer);

        return Ok(customerToReturn);
    }
        
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, DeliveryCustomerInfo customer)
    {
        if (id != customer.Id)
            return BadRequest("Customer ID mismatch");

        var updatedCustomer = await _deliveryCustomerService.UpdateCustomerAsync(customer);
        return Ok(updatedCustomer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var result = await _deliveryCustomerService.DeleteCustomerAsync(id);
        if (!result)
            return NotFound("Customer not found");

        return NoContent();
    }


    [HttpPost("add-new-customer-address")]
    public async Task<IActionResult> AddNewCustomerAddress(CustomerNewAddressDto customerAddress)
    {
        var customerAddressToCreate = _mapper.Map<CustomerNewAddressDto, CustomerAddress>(customerAddress);
        var createdCustomer = await _deliveryCustomerService.AddNewCustomerAddressAsync(customerAddress.FirstPhoneNumber!, customerAddressToCreate);

        if (createdCustomer is null)
            return BadRequest("Failed to add customer address.");

        return Ok(customerAddress);
    }
}