namespace POS.Contract.Dtos.Common;

public class BaseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
