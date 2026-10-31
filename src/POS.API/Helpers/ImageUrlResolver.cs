namespace POS.API.Helpers;

public class ImageUrlResolver<T,TEntity> : IValueResolver<T, TEntity, string?>
{
    private readonly IConfiguration _configuration;

    public ImageUrlResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? Resolve(T source ,  TEntity destination, string? destMember, ResolutionContext context)
    {
        var imagePath = source?.GetType().GetProperty("ImagePath")?.GetValue(source, null) as string;

        if (imagePath != null)
        {
            return $"{_configuration["ApiBaseUrl"]}/{imagePath}";
        }
        else
            return "";
    }
}