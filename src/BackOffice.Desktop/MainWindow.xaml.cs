using BlazorBase;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace BackOffice.Desktop;

public partial class MainWindow : Window
{
    private readonly CommonProperties? _commonProperties;

    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;
    }

    public MainWindow(IServiceProvider serviceProvider) : this()
    {
        try
        {
            Serilog.Log.Information("MainWindow constructor called with ServiceProvider");
            blazorWebView.Services = serviceProvider;
            _commonProperties = serviceProvider.GetRequiredService<CommonProperties>();
            Serilog.Log.Information("MainWindow initialized successfully");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in MainWindow constructor");
            throw;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_commonProperties != null && _commonProperties.TableItems != null && _commonProperties.TableItems.Any())
        {
            MessageBox.Show("????? ????? ????? ?????? ????? ??? ????? ????????.", "?????", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Cancel = true;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (this.WindowState != WindowState.Minimized)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
