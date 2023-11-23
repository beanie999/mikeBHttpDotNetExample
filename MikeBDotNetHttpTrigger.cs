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

        [NewRelic.Api.Agent.Trace]
        private void generateError(NewRelic.Api.Agent.ITransaction transaction) {
            Random rnd = new Random();
            int num = rnd.Next(10);
            _logger.LogInformation($"Random number was {num}.");
            transaction.AddCustomAttribute("randomNumber", num);
            int crash = 10/num;
        }

        [NewRelic.Api.Agent.Trace]
        private void waitSome(NewRelic.Api.Agent.ITransaction transaction) {
            Random rnd = new Random();
            int num = rnd.Next(1000);
            _logger.LogInformation($"Wait time in milli seconds was {num}.");
            transaction.AddCustomAttribute("sleepTime", num);
            Thread.Sleep(num);
        }

        [Function("MikeBDotNetHttpTrigger")]
        [NewRelic.Api.Agent.Transaction(Web = true)]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            NewRelic.Api.Agent.NewRelic.SetTransactionUri(req.Url);
            NewRelic.Api.Agent.IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
            NewRelic.Api.Agent.ITransaction transaction = agent.CurrentTransaction;

            this.generateError(transaction);
            this.waitSome(transaction);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
