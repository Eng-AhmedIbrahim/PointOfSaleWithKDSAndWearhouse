using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PdfiumViewer;

namespace Warehouse.Desktop.Services;

public interface IDesktopPrintingService
{
    void PrintPdf(byte[] pdfData, string? printerName = null);
    void PrintWithPrinterDialog(byte[] pdfData);
    List<string> GetInstalledPrinters();
    void SaveFileWithDialog(byte[] fileData, string defaultFileName, string filter, string title = "حفظ الملف");
    string? SaveFile(byte[] fileData, string defaultFileName, string filter, string title = "حفظ الملف", bool openAfterSave = false);
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
        SaveFile(fileData, defaultFileName, filter, title, false);
    }

    public string? SaveFile(byte[] fileData, string defaultFileName, string filter, string title = "حفظ الملف", bool openAfterSave = false)
    {
        return Application.Current.Dispatcher.Invoke(() =>
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
            {
                File.WriteAllBytes(dialog.FileName, fileData);

                if (openAfterSave)
                {
                    try
                    {
                        var psi = new ProcessStartInfo(dialog.FileName) { UseShellExecute = true };
                        Process.Start(psi);
                    }
                    catch
                    {
                        // Ignore if default app couldn't be opened
                    }
                }
                return dialog.FileName;
            }
            return null;
        });
    }
}
