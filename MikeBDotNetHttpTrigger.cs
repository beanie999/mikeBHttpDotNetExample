using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace NewRelic.Function;

public class MikeBDotNetHttpTrigger
{
    private readonly ILogger _logger;
    private const string userParam = "user";

    public MikeBDotNetHttpTrigger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MikeBDotNetHttpTrigger>();
    }

    // Method to setup some stuff for New Relic.
    private NewRelic.Api.Agent.ITransaction helpNewRelic(HttpRequestData req)
    {
        // Ensure the Transaction Uri is set correctly.
        NewRelic.Api.Agent.NewRelic.SetTransactionUri(req.Url);
        // Accept the W3C trace headers if they are passed in.
        NewRelic.Api.Agent.IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        NewRelic.Api.Agent.ITransaction transaction = agent.CurrentTransaction;
        IEnumerable<string> Getter(HttpRequestData carrier, string key)
        {
            return carrier.Headers.TryGetValues(key, out var values) ? new string[] { values.First() } : null;
        }
        transaction.AcceptDistributedTraceHeaders(req, Getter, NewRelic.Api.Agent.TransportType.HTTP);

        return transaction;
    }

    // Method to generate a random error. This will be traced within New Relic.
    [NewRelic.Api.Agent.Trace]
    private void generateError(NewRelic.Api.Agent.ITransaction transaction)
    {
        Random rnd = new Random();
        int num = rnd.Next(10);
        _logger.LogInformation($"Random number was {num}.");
        // Add the random number as a custom attribute in New Relic.
        transaction.AddCustomAttribute("randomNumber", num);
        int crash = 10 / num;
    }

    // Method to add a random wait time. Traced within New Relic.
    [NewRelic.Api.Agent.Trace]
    private void waitSome(NewRelic.Api.Agent.ITransaction transaction)
    {
        Random rnd = new Random();
        int num = rnd.Next(1000);
        _logger.LogInformation($"Wait time in milli seconds was {num}.");
        // Add the sleep time as a custom attribute in New Relic.
        transaction.AddCustomAttribute("sleepTime", num);
        Thread.Sleep(num);
    }

    // Main method, will appear in New Relic as a web transaction.
    [Function("MikeBDotNetHttpTrigger")]
    [NewRelic.Api.Agent.Transaction(Web = true)]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        NewRelic.Api.Agent.ITransaction transaction = helpNewRelic(req);
        _logger.LogInformation("MikeBDotNetHttpTrigger called.");
        // Log all the headers, shows the W3C headers.
        foreach (var header in req.Headers)
        {
            _logger.LogInformation(String.Format("Header {0}={1}", header.Key, String.Join(",", header.Value)));
        }

        // Check if we were past a user parameter.
        if (req.Query.GetValues(userParam) != null && !String.IsNullOrEmpty(req.Query.GetValues(userParam).First()))
        {
            string user = req.Query.GetValues(userParam).First();
            _logger.LogInformation($"User is: {user}");
            // Set the userId in New Relic for Errors Inbox.
            transaction.SetUserId(user);
        }

        // Random error and wait time.
        this.generateError(transaction);
        this.waitSome(transaction);

        // Build the response.
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString("Welcome to Azure Functions!");

        _logger.LogInformation("MikeBDotNetHttpTrigger finished.");

        return response;
    }
}
