using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using PdfiumViewer;
using System.Drawing.Printing;

namespace CashierPrinterApp
{
    class Program
    {
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var printerHubUrl = config["SignalR:PrinterHubUrl"];

            string machineName = Environment.MachineName; // أو استخدم Dns.GetHostName()

            var connection = new HubConnectionBuilder()
                .WithUrl(printerHubUrl ?? string.Empty)
                .Build();

            connection.On<string, string, string>("ReceivePrintJob", async (printerName, pdfFilePath, numberOfCopiesStr) =>
            {
                if (int.TryParse(numberOfCopiesStr, out int numberOfCopies))
                {
                    await PrintPdfAsync(pdfFilePath, printerName, numberOfCopies);
                }
                else
                {
                    Console.WriteLine($"Invalid number of copies: {numberOfCopiesStr}");
                }
            });

            await connection.StartAsync();

            // تخزين ConnectionId مع اسم الجهاز في Redis عبر SignalR
            await connection.SendAsync("RegisterDevice", machineName);

            Console.WriteLine("Connected to the printer hub. Waiting for print jobs...");
            Console.ReadLine();
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static Task PrintPdfAsync(string pdfFilePath, string printerName, int numberOfCopies)
        {
            return Task.Run(() =>
            {
                using (var document = PdfDocument.Load(pdfFilePath))
                using (var printDoc = document.CreatePrintDocument())
                {
                    printDoc.PrinterSettings = new PrinterSettings
                    {
                        PrinterName = printerName,
                        Copies = (short)numberOfCopies
                    };

                    printDoc.DefaultPageSettings.Landscape = false;
                    printDoc.PrintController = new StandardPrintController();

                    printDoc.Print();
                }
            });
        }
    }
}