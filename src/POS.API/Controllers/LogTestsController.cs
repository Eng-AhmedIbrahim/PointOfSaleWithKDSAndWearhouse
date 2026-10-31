namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LogTestsController : ControllerBase
{
    private static readonly List<Book> Books =
        [
            new(){Id = 1001, Title = "ASP.NET Core", Author = "Pranaya", YearPublished = 2019},
            new(){Id = 1001, Title = "SQL Server", Author = "Pranaya", YearPublished = 2022}
        ];

    private readonly ILogger<LogTestsController> _logger;

    public LogTestsController(ILogger<LogTestsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogTrace("This is a Trace log, the most detailed information.");
        _logger.LogDebug("This is a Debug log, useful for debugging.");
        _logger.LogInformation("This is an Information log, general info about app flow.");
        _logger.LogWarning("This is a Warning log, indicating a potential issue.");
        _logger.LogError("This is an Error log, indicating a failure in the current operation.");
        _logger.LogCritical("This is a Critical log, indicating a serious failure in the application.");
        return Ok("Check your logs to see the different logging levels in action!");
    }

    [HttpPost("books")]
    public IActionResult AddBook([FromBody] Book book)
    {
        Books.Add(book);
        //Structured Logging, i.e., JSON Format
        _logger.LogInformation("Added a new book {@Book}", book);
        return Ok();
    }
    [HttpGet("books")]
    public IActionResult GetBooks()
    {
        //Structured Logging, i.e., JSON Format
        _logger.LogInformation("Retrieved all books. Books: {@Books}", Books);
        return Ok(Books);
    }
}

public class Book
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int YearPublished { get; set; }
}
