using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace NewRelic.Function
{
    public class MikeBDotNetHttpTrigger
    {
        private readonly ILogger _logger;

        public MikeBDotNetHttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MikeBDotNetHttpTrigger>();
        }

        [Function("MikeBDotNetHttpTrigger")]
        [NewRelic.Api.Agent.Transaction(Web = true)]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            NewRelic.Api.Agent.NewRelic.SetTransactionUri(req.Url);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
