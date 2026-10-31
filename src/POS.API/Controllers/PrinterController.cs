namespace POS.API.Controllers;

public class PrinterController : BaseApiController
{
    private readonly IPrinterServices _printerServices;

    public PrinterController(IPrinterServices printerServices)
    {
        _printerServices = printerServices;
    }

    [HttpPost("order-setting")]
    public async Task<ActionResult<OrderSetting>> CreateOrderSettingAsync([FromBody] OrderSetting orderSetting)
    {
        var result = await _printerServices.CreateOrderSettingAsync(orderSetting);
        if (result == null)
        {
            return BadRequest("Unable to create OrderSetting");
        }
        return Ok(result);
    }

    [HttpPost("printer")]
    public async Task<ActionResult<KitchenType>> CreatePrinterAsync([FromBody] KitchenType printer)
    {
        var result = await _printerServices.CreatePrinterAsync(printer);
        if (result == null)
        {
            return BadRequest("Unable to create Printer");
        }
        return Ok(result);
    }

    [HttpGet("kitchen/{id}")]
    public async Task<ActionResult<KitchenType>> GetKitchenByIdAsync(int id)
    {
        var result = await _printerServices.GetKitchenByIdAsync(id);
        if (result == null)
        {
            return NotFound("Kitchen not found");
        }
        return Ok(result);
    }

    [HttpGet("kitchens")]
    public async Task<ActionResult<List<KitchenType>>> GetKitchenTypesAsync([FromQuery] string? deviceName = null)
    {
        var result = await _printerServices.GetKitchenTypesAsync(deviceName);
        if (result == null || result.Count == 0)
        {
            return NoContent();
        }
        return Ok(result);
    }

    [HttpGet("order-setting/{id}")]
    public async Task<ActionResult<OrderSetting>> GetOrderSettingByIdAsync(int id)
    {
        var result = await _printerServices.GetOrderSettingByIdAsync(id);
        if (result == null)
        {
            return NotFound("OrderSetting not found");
        }
        return Ok(result);
    }

    [HttpGet("order-settings")]
    public async Task<ActionResult<List<OrderSetting>>> GetOrderSettingsAsync([FromQuery] string? deviceName = null)
    {
        var result = await _printerServices.GetOrderSettingsAsync(deviceName);
        if (result == null || result.Count == 0)
        {
            return NoContent();
        }
        return Ok(result);
    }

    [HttpPut("printer")]
    public async Task<ActionResult<KitchenType>> UpdatePrinterAsync([FromBody] KitchenType printer)
    {
        var result = await _printerServices.UpdateKitchenTypesAsync(printer);
        if (result == null)
        {
            return BadRequest("Unable to update Printer");
        }
        return Ok(result);
    }

    [HttpPut("order-setting")]
    public async Task<ActionResult<OrderSetting>> UpdateOrderSettingAsync([FromBody] OrderSetting orderSetting)
    {
        var result = await _printerServices.UpdateOrderSettingAsync(orderSetting);
        if (result == null)
        {
            return BadRequest("Unable to update OrderSetting");
        }
        return Ok(result);
    }

    [HttpGet("installed-printers")]
    public async Task<ActionResult<List<string>>> GetInstalledPrinters()
    {
        var printers = await _printerServices.GetInstalledPrinters();
        return Ok(printers);
    }

    [HttpPost("print-pdf")]
    public async Task<ActionResult> PrintPdfAsync([FromQuery] string pdfFilePath, [FromQuery] string printerName)
    {
        try
        {
            await _printerServices.PrintPdfAsync(pdfFilePath, printerName);
            return Ok("Print job started");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error printing PDF: {ex.Message}");
        }
    }

    [HttpPost("print-pdf-multiple")]
    public async Task<ActionResult> PrintPdfToMultipleAsync([FromQuery] string pdfFilePath, [FromBody] List<string> printerNames)
    {
        try
        {
            await _printerServices.PrintPdfToMultipleAsync(pdfFilePath, printerNames);
            return Ok("Print job started on multiple printers");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error printing PDF to multiple printers: {ex.Message}");
        }
    }
}
