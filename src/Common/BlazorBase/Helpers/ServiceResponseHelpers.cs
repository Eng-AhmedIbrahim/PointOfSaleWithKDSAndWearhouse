namespace BlazorBase.Helpers;

public static class ServiceResponseHelpers
{
    public static async Task<ServiceResponse<T>> ExecuteWithResponseAsync<T>
        (Func<Task<ServiceResponse<T>>> action, string errorMessage)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Failure<T>(errorMessage);
        }
    }

    public static ServiceResponse<T> NotFound<T>(string message)
    {
        return new ServiceResponse<T> { Success = false, Message = message };
    }

    public static ServiceResponse<T> Success<T>(T data, string message)
    {
        return new ServiceResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ServiceResponse<T> Failure<T>(string message)
    {
        return new ServiceResponse<T> { Success = false, Message = message };
    }
}
