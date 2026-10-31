using POS.Contract.Models.ReceiptModels.DineIn;

namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly string _reportsFolder;

    public ReportController(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
        _reportsFolder = Path.Combine(_hostEnvironment.ContentRootPath, "Reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    [HttpGet("Generate-TakeAway-Receipt")]
    public async Task<IActionResult> GenerateReceiptReport()
    {
        var receipt = FakeDataGenerator.GenerateFakeReceipt(20);
        var document = await Task.Run(() => new ReceiptDocument(receipt.receipt, receipt.tableItems));

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-receipt.pdf");

        document.GeneratePdf(outputPath);

        return Ok($"Report generated successfully: {outputPath}");
    }

    [HttpGet("Generate-Delivery-Receipt")]
    public async Task<IActionResult> GenerateDeliveryReceiptReport()
    {
        var receiptData = FakeDeliveryDataGenerator.GenerateFakeReceipt(10);

        var document = await Task.Run(() =>
            new DeliveryReceiptDocument(receiptData.receipt, receiptData.tableItems));

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-delivery-receipt.pdf");

        document.GeneratePdf(outputPath);

        return Ok($"Report generated successfully: {outputPath}");
    }

    [HttpGet("Generate-Kitchen-Receipt")]
    public async Task<IActionResult> GenerateKitchenReceiptReport()
    {
        var receiptData = FakeKitchenDataGenerator.GenerateFakeReceipt(5);

        var document = await Task.Run(() =>
            new KitchenReceiptDocument(receiptData.receipt, receiptData.tableItems));

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-kitchen-receipt.pdf");

        document.GeneratePdf(outputPath);

        return Ok($"Report generated successfully: {outputPath}");
    }


    [HttpGet("Generate-DineIn-Receipt")]
    public async Task<IActionResult> GenerateDineInReceiptReport()
    {
        var receiptData  = FakeDineInDataGenerator.Generate(5);

        var document = await Task.Run(() =>
            new DineInReceiptDocument(receiptData.receipt, receiptData.tableItems));

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-DineIn-receipt.pdf");

        document.GeneratePdf(outputPath);

        return Ok($"Report generated successfully: {outputPath}");
    }
}