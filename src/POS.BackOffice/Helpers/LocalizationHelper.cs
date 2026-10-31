namespace POS.BackOffice.Helpers;

public static class LocalizationHelper
{
    public static RequestLocalizationOptions GetLocalizationOptions(IConfiguration configuration)
    {
        var cultures = configuration.GetSection("Cultures").GetChildren()
            .ToDictionary(x => x.Key, x => x.Value);

        var supportedCultures = cultures.Keys.ToArray();

        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        return localizationOptions;
    }
}
