using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;
using System.Threading;
using BlazorBase;
using BlazorBase.ERPFrontServices.PrintOrderServices;
using POS.Desktop.Auth;
using POS.Desktop.Extensions;
using MudBlazor.Services;
using Blazored.LocalStorage;
using System.Text.Json;
using BlazorBase.ERPFrontServices.Section4ButtonsService;
using BlazorBase.ERPFrontServices.CartServices;
using BlazorBase.ERPFrontServices.CategoryServices;
using BlazorBase.ERPFrontServices.AppDateServices;
using BlazorBase.ERPFrontServices.DineInServices;
using BlazorBase.ERPFrontServices.OrderServices;
using BlazorBase.ERPFrontServices.DeliveryServices;
using BlazorBase.ERPFrontServices.BranchServices;
using BlazorBase.ERPFrontServices.DineInOrderServices;
using BlazorBase.ERPFrontServices.OrderTrackServices;
using BlazorBase.ERPFrontServices.ComplaintServices;
using BlazorBase.ERPFrontServices.VoidServices;
using BlazorBase.ERPFrontServices.ReportingServices;
using BlazorBase.ERPFrontServices.PaymentMethodServices;
using BlazorBase.ERPFrontServices.SettingsServices;
using BlazorBase.ERPFrontServices.InventoryServices;
using BlazorBase.API;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using POS.Authorization.Models;
using POS.Desktop.Components;
using System.Net.Http;
using POS.Desktop.Services;
using POS.Core.Services.Contract.PrinterServices;
using POS.Core.Services.Contract.PosFeatureServices;
using Microsoft.Extensions.Localization;
using ERPFront.HubSettings;
using ERPFront.Models;
using POS.Core.Services.Contract;
using POS.Core.Entities.Identity;
using POS.Services.StaffMealServices;
using BlazorBase.ERPFrontServices.StaffMealServices;
using BlazorBase.ERPFrontServices.LicensingServices;


namespace POS.Desktop;

public partial class App : Application
{
    private static Mutex? _instanceMutex;
    private ServiceProvider? _serviceProvider;
    public static List<string> InMemoryLogs { get; } = new();
    private static readonly object _logLock = new();

    public static void AddLog(string message)
    {
        lock (_logLock)
        {
            InMemoryLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (InMemoryLogs.Count > 500) InMemoryLogs.RemoveAt(0);
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "Global\\StorePOS_Desktop_SingleInstance";
        _instanceMutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("نسخة من تطبيق POS مفتوحة بالفعل على هذا الجهاز. الرجاء إغلاق النسخة القديمة قبل المحاولة مرة أخرى.", 
                            "تكرار التشغيل", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return;
        }

        try
        {
            // Register global exception handlers
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            base.OnStartup(e);

            // Setup Serilog for file logging
            SetupSerilog();

            Log.Information("Setting up services...");
            AddLog("Application Starting Store POS Desktop");

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var callCenterService = _serviceProvider.GetRequiredService<CallCenterNotificationService>();
            _ = Task.Run(async () => await callCenterService.InitializeAsync());

            Log.Information("Checking license...");
            var licenseService = _serviceProvider.GetRequiredService<ILicenseService>();
            
            bool isLicensed = false;
            try
            {
                isLicensed = Task.Run(async () => await licenseService.IsLicensedAsync()).GetAwaiter().GetResult();
            }
            catch (Exception licEx)
            {
                Log.Error(licEx, "License check threw an exception");
                var logPath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "POS-Logs");
                Directory.CreateDirectory(logPath2);
                File.WriteAllText(Path.Combine(logPath2, $"license-error-{DateTime.Now:yyyyMMdd-HHmmss}.txt"), licEx.ToString());
            }

            if (!isLicensed)
            {
                MessageBox.Show("هذا الجهاز غير مرخص. يرجى تسجيل الجهاز وتفعيله من خلال برنامج الإدارة (BackOffice) قبل استخدام نقاط البيع.", 
                                "ترخيص مفقود أو غير صالح", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            Log.Information("Creating main window...");


            // Create and show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("Main window shown successfully");
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "POS-Logs");
            Directory.CreateDirectory(logPath);
            
            var errorFile = Path.Combine(logPath, $"startup-error-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(errorFile, $"Startup Error:\n{ex.ToString()}");
            
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Failed to start application:\n{ex.Message}\n\nCheck logs at: {logPath}", 
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void SetupSerilog()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "POS-Logs");
        Directory.CreateDirectory(logPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.Sink(new DelegatingSink(AddLog)) // Custom sink to push logs to our list
            .WriteTo.File(
                Path.Combine(logPath, "POS-Desktop-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Application starting up - Logs saved to: {LogPath}", logPath);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled UI Exception");
        ShowErrorDialog("UI Thread Error", e.Exception);
        e.Handled = true; // Prevent app from closing immediately
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled Domain Exception");
        if (ex != null) ShowErrorDialog("Domain Error", ex);
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unobserved Task Exception");
        ShowErrorDialog("Background Task Error", e.Exception);
        e.SetObserved();
    }

    private void ShowErrorDialog(string title, Exception ex)
    {
        Dispatcher.Invoke(() =>
        {
            var message = $"Critical Error: {ex.Message}\n\nType: {ex.GetType().Name}\n\nStack Trace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                message += $"\n\nInner Exception: {ex.InnerException.Message}";
            }
            
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    // Helper sink for Serilog
    private class DelegatingSink : Serilog.Core.ILogEventSink
    {
        private readonly Action<string> _logAction;
        public DelegatingSink(Action<string> logAction) => _logAction = logAction;
        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            if (logEvent.Exception != null) message += $" | EX: {logEvent.Exception.Message}";
            _logAction(message);
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });

        // Add WPF services
        services.AddSingleton<MainWindow>();

        // Add Blazor services
        services.AddWpfBlazorWebView();
        services.AddBlazorWebViewDeveloperTools();
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });
        services.AddBlazorBootstrap();
        services.AddBlazoredLocalStorage();

        // Localization
        services.AddLocalization();

        services.AddScoped<BlazorBase.Services.LocalizationService, DesktopLocalizationService>();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Configure supported cultures
        var culture = new System.Globalization.CultureInfo("ar");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

        // API Settings
        services.Configure<BlazorBase.API.ApiSettings>(configuration.GetSection("ApiSettings"));
        services.AddSingleton<BlazorBase.API.ApiSettings>(sp =>
            sp.GetRequiredService<IOptions<BlazorBase.API.ApiSettings>>().Value);

        // HTTP Client
        services.AddHttpClient(configuration["ApiSettings:ApiName"]!,
            client => { client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!); })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = true,
                AllowAutoRedirect = true
            });

        // Register default HttpClient as well for services that don't specify a name
        services.AddScoped(sp => 
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient(configuration["ApiSettings:ApiName"]!);
        });

        // Application services
        services.AddSingleton<CommonProperties>();
        services.AddSingleton<HandelDeliveryInvocation>();
        services.AddSingleton<CartService>();
        services.AddSingleton<Section4ButtonsServices>();
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
        services.AddScoped<IDineInService, DineInService>();
        services.AddScoped<IAppDateService, AppDateService>();
        services.AddScoped<IOrderSettingsService, OrderSettingsService>();
        services.AddScoped<IDeliveryServices, DeliveryServices>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IPrintOrderService, DesktopPrintOrderService>();
        services.AddSingleton<CallCenterNotificationService>();

        // Category services
        services.AddScoped<BlazorBase.ERPFrontServices.CategoryServices.ICategoryServices, BlazorBase.ERPFrontServices.CategoryServices.CategoryService>();
        services.AddScoped<BlazorBase.ERPFrontServices.DineInOrderServices.IDineInOrderFrontService, BlazorBase.ERPFrontServices.DineInOrderServices.DineInOrderFrontService>();
        services.AddScoped<BlazorBase.ERPFrontServices.OrderTrackServices.IOrderTrackFrontService, BlazorBase.ERPFrontServices.OrderTrackServices.OrderTrackFrontService>();
        services.AddScoped<BlazorBase.ERPFrontServices.ComplaintServices.IComplaintServices, BlazorBase.ERPFrontServices.ComplaintServices.ComplaintServices>();
        services.AddScoped<BlazorBase.ERPFrontServices.AccountServices.IAccountService, BlazorBase.ERPFrontServices.AccountServices.AccountService>();
        services.AddScoped<BlazorBase.ERPFrontServices.ItemServices.IItemService, BlazorBase.ERPFrontServices.ItemServices.ItemService>();
        services.AddScoped<IStaffMealService, StaffMealFrontService>();
        services.AddScoped<BlazorBase.ERPFrontServices.DistributionServices.IDistributionErpService, BlazorBase.ERPFrontServices.DistributionServices.DistributionErpService>();
        services.AddScoped<IVoidErpService, VoidErpService>();
        services.AddScoped<IReportingErpService, ReportingErpService>();
        services.AddScoped<IPaymentMethodServices, BlazorBase.ERPFrontServices.PaymentMethodServices.PaymentMethodServices>();
        services.AddScoped<ISystemSettingsServices, SystemSettingsServices>();
        services.AddScoped<IInventoryFrontService, InventoryFrontService>();
        services.AddScoped<IRecipeFrontService, RecipeFrontService>();
        services.AddScoped<IUnitFrontService, UnitFrontService>();
        services.AddScoped<ILicenseService, LicenseService>();


        // Call Center Hub Settings
        services.Configure<CallCenterHubSettings>(configuration.GetSection("CallCenterHubs"));
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<CallCenterHubSettings>>().Value);
        
        // Dispatcher Settings
        services.Configure<DispatcherSettings>(configuration.GetSection("DispatcherSettings"));
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<DispatcherSettings>>().Value);
        
        // Desktop-specific services (file-based storage instead of localStorage)
        services.AddSingleton<DesktopFileStorageService>();
        services.AddScoped<DesktopFileStorageWrapper>(sp =>
        {
            var fileStorage = sp.GetRequiredService<DesktopFileStorageService>();
            return new DesktopFileStorageWrapper(fileStorage);
        });
        services.AddScoped<ICustomizationSettingsService, DesktopCustomizationSettingsService>();
        services.AddScoped<DesktopFontSizeService>();

        // Local Storage
        services.AddBlazoredLocalStorage(config =>
        {
            config.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            config.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            config.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
            config.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            config.JsonSerializerOptions.WriteIndented = false;
        });

        services.AddScoped<IPosFeatureSettingsService, DesktopPosFeatureSettingsService>();
        services.AddScoped<IPrinterServices, DesktopPrinterService>();
        services.AddScoped<IDesktopPrintingService, DesktopPrintingService>();

        // Authentication & Authorization
        services.AddAuthenticationCore();
        services.AddAuthorization(options =>
        {
            foreach (var policy in Permissions.RolePermissions)
            {
                options.AddPolicy(policy.Key, policyBuilder =>
                    policyBuilder.RequireAssertion(context =>
                    {
                        var hasPermission = context.User.HasClaim(c => 
                            c.Type.Equals("Permission", StringComparison.OrdinalIgnoreCase) && 
                            c.Value == policy.Value);
                        
                        var isDenied = context.User.HasClaim(c => 
                            c.Type.Equals("deny", StringComparison.OrdinalIgnoreCase) && 
                            c.Value == policy.Value);

                        return hasPermission && !isDenied;
                    }));
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
