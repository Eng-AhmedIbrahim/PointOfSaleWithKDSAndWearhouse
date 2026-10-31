using System.Drawing.Printing;
using System.Windows;
using Microsoft.Win32;
using PdfiumViewer;
using System.IO;

namespace POS.Desktop.Services;

public interface IDesktopPrintingService
{
    void PrintPdf(byte[] pdfData, string? printerName = null);
    void PrintWithPrinterDialog(byte[] pdfData);
    List<string> GetInstalledPrinters();
    void SaveFileWithDialog(byte[] fileData, string defaultFileName, string filter, string title = "حفظ الملف");
}

public class DesktopPrintingService : IDesktopPrintingService
{
    public void PrintPdf(byte[] pdfData, string? printerName = null)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.Invoke(() =>
        {
            try
            {
                using var ms = new MemoryStream(pdfData);
                using var pdfDocument = PdfDocument.Load(ms);
                using var printDocument = pdfDocument.CreatePrintDocument();
                
                if (!string.IsNullOrEmpty(printerName))
                    printDocument.PrinterSettings.PrinterName = printerName;
                
                printDocument.Print();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Printing failed: {ex.Message}");
                throw;
            }
        });
    }

    public void PrintWithPrinterDialog(byte[] pdfData)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            using var ms = new MemoryStream(pdfData);
            using var pdfDocument = PdfDocument.Load(ms);
            using var printDocument = pdfDocument.CreatePrintDocument();

            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDocument.PrinterSettings.PrinterName = printDialog.PrintQueue.FullName;
                printDocument.Print();
            }
        });
    }

    public List<string> GetInstalledPrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
            printers.Add(printer);
        return printers;
    }

    public void SaveFileWithDialog(byte[] fileData, string defaultFileName, string filter, string title = "حفظ الملف")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                FileName = defaultFileName,
                Filter = filter,
                DefaultExt = System.IO.Path.GetExtension(defaultFileName).TrimStart('.'),
                OverwritePrompt = true,
                AddExtension = true,
            };

            if (dialog.ShowDialog() == true)
                File.WriteAllBytes(dialog.FileName, fileData);
        });
    }
}
