using FastReport;
using FastReport.Export.PdfSimple;
using POS.Contract.Dtos.ReportingDtos;
using System.Data;
using System.Linq;

namespace POS.Reports;

public interface IFastReportService
{
    byte[] GeneratePdf(string templatePath, DataSet data, Dictionary<string, string>? parameters = null);
    byte[] GenerateExcel(string templatePath, DataSet data, Dictionary<string, string>? parameters = null);
}

public class FastReportService : IFastReportService
{
    public byte[] GeneratePdf(string templatePath, DataSet data, Dictionary<string, string>? parameters = null)
    {
        using var report = new Report();
        report.Load(templatePath);
        
        // Register the dataset instead of individual tables for correct ReferenceName binding
        report.RegisterData(data, data.DataSetName);
        
        foreach (DataTable table in data.Tables)
        {
            var dataSource = report.GetDataSource(table.TableName);
            if (dataSource != null)
            {
                dataSource.Enabled = true;
            }
        }

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                report.SetParameterValue(param.Key, param.Value);
            }
        }

        // Programmatically control image band height to avoid empty space
        bool hasImageData = data.Tables.Cast<DataTable>()
            .Any(t => t.Columns.Contains("HasImage"));
        if (hasImageData)
        {
            var dataBand = report.FindObject("Data1") as FastReport.DataBand;
            var picObj   = report.FindObject("Pic1")   as FastReport.PictureObject;
            if (dataBand != null && picObj != null)
            {
                const float normalHeight = 65f;
                const float imageHeight  = 175f;
                dataBand.BeforePrint += (_, _) =>
                {
                    bool show = false;
                    try { show = (bool)(report.GetColumnValue("Transactions.HasImage") ?? false); } catch { }
                    picObj.Visible   = show;
                    dataBand.Height  = show ? imageHeight : normalHeight;
                };
            }
        }

        report.Prepare();

        using var ms = new MemoryStream();
        var export = new PDFSimpleExport();
        export.Export(report, ms);
        return ms.ToArray();
    }

    public byte[] GenerateExcel(string templatePath, DataSet data, Dictionary<string, string>? parameters = null)
    {
        throw new NotImplementedException("Excel export is handled via ClosedXML in ReportsManager.");
    }
}
