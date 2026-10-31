// using DevExpress.XtraReports.UI;
// using System.Drawing.Printing;
//
// namespace POS.API.Controllers;
//
// [Route("api/[controller]")]
// [ApiController]
// public class ReportController : ControllerBase
// {
//     private readonly IWebHostEnvironment _hostEnvironment;
//     private readonly AppDbContext _dbContext;
//
//     public ReportController(IWebHostEnvironment hostEnvironment, AppDbContext dbContext)
//     {
//         _hostEnvironment = hostEnvironment;
//         _dbContext = dbContext;
//     }
//
//     /// <summary>
//     /// Generates a receipt report as a PDF.
//     /// </summary>
//     [HttpGet("GenerateReceipt")]
//     public async Task<ActionResult> GenerateReceiptReport()
//     {
//         var reportPath = GetReportPath("TakeAwayReport.repx");
//         if (!System.IO.File.Exists(reportPath))
//         {
//             return NotFound("Report file not found: " + reportPath);
//         }
//
//         var pdf = GenerateReportPdf(reportPath, GetSampleReceiptDetails());
//         return File(pdf, "application/pdf", "ReceiptReport.pdf");
//     }
//
//     /// <summary>
//     /// Returns a list of installed printers.
//     /// </summary>
//     [HttpGet("Printers")]
//     public ActionResult<IEnumerable<string>> GetInstalledPrinters()
//     {
//         return Ok(PrinterSettings.InstalledPrinters.Cast<string>());
//     }
//
//     /// <summary>
//     /// Prints a receipt report directly to a specified printer.
//     /// </summary>
//     [HttpPost("PrintReceipt")]
//     public ActionResult PrintReceipt([FromBody] PrintRequest request)
//     {
//         if (string.IsNullOrWhiteSpace(request.PrinterName))
//             return BadRequest("Printer name is required.");
//
//         var reportPath = GetReportPath("TakeAwayReport.repx");
//         if (!System.IO.File.Exists(reportPath))
//             return NotFound("Report file not found: " + reportPath);
//
//         var receiptDetails = GetSampleReceiptDetails();
//         PrintReport(reportPath, receiptDetails, request.PrinterName);
//         return Ok("Print job sent successfully.");
//     }
//
//     /// <summary>
//     /// Loads a DevExpress report and exports it to PDF.
//     /// </summary>
//     private byte[] GenerateReportPdf(string reportPath, TakeAwayReceiptDetails data)
//     {
//         XtraReport report = new XtraReport();
//         report.LoadLayout(reportPath);
//         report.DataSource = new List<TakeAwayReceiptDetails> { data };
//
//         using var memoryStream = new MemoryStream();
//         report.ExportToPdf(memoryStream);
//         return memoryStream.ToArray();
//     }
//
//     /// <summary>
//     /// Prints the report to a specified printer.
//     /// </summary>
//     private void PrintReport(string reportPath, TakeAwayReceiptDetails data, string printerName = "Bullzip PDF Printer")
//     {
//         XtraReport report = new XtraReport();
//         report.LoadLayout(reportPath);
//         report.DataSource = new List<TakeAwayReceiptDetails> { data };
//
//         if (!PrinterSettings.InstalledPrinters.Cast<string>().Contains(printerName))
//             throw new Exception($"Printer '{printerName}' not found.");
//
//         report.PrinterName = printerName;
//         report.Print();
//     }
//
//     /// <summary>
//     /// Retrieves the full path to the report file.
//     /// </summary>
//     private string GetReportPath(string reportFileName)
//     {
//         return Path.Combine(_hostEnvironment.WebRootPath, "Reports", reportFileName);
//     }
//
//     /// <summary>
//     /// Returns sample receipt data.
//     /// </summary>
//     private TakeAwayReceiptDetails GetSampleReceiptDetails()
//     {
//         return new TakeAwayReceiptDetails
//         {
//             BranchName = "Example Branch",
//             CashierName = "John Doe",
//             OrderDate = "2025-02-23",
//             OrderTime = "14:30:00",
//             OrderNumber = 1234,
//             OrderType = "TakeAway",
//             OrderItems = new List<TableItem>
//             {
//                 new TableItem { Name = "Pizza", Quantity = 1, Price = 15.99m, Total = 15.99m },
//                 new TableItem { Name = "Soda", Quantity = 2, Price = 1.99m, Total = 3.98m }
//             }
//         };
//     }
// }
//
// /// <summary>
// /// Represents the receipt details.
// /// </summary>
// public class TakeAwayReceiptDetails
// {
//     public string BranchName { get; set; }
//     public string CashierName { get; set; }
//     public string OrderDate { get; set; }
//     public string OrderTime { get; set; }
//     public int OrderNumber { get; set; }
//     public string OrderType { get; set; }
//     public List<TableItem> OrderItems { get; set; }
// }
//
// /// <summary>
// /// Represents an item in the receipt.
// /// </summary>
// public class TableItem
// {
//     public string Name { get; set; }
//     public int Quantity { get; set; }
//     public decimal Price { get; set; }
//     public decimal Total { get; set; }
// }
//
// /// <summary>
// /// Represents a print request.
// /// </summary>
// public class PrintRequest
// {
//     public string PrinterName { get; set; }
// }
