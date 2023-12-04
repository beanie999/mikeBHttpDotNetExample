using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
namespace NewRelic.Function;

using System;
using System.Collections.Generic;


    public class MikeBDotNetHttpTrigger
    {
        private readonly ILogger _logger;

        public MikeBDotNetHttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MikeBDotNetHttpTrigger>();
        }

        private NewRelic.Api.Agent.ITransaction helpNewRelic(HttpRequestData req) {
            NewRelic.Api.Agent.NewRelic.SetTransactionUri(req.Url);
            NewRelic.Api.Agent.IAgent agent = NewRelic.Api.Agent.NewRelic.GetAgent();
            NewRelic.Api.Agent.ITransaction transaction = agent.CurrentTransaction;
            IEnumerable<string> Getter(HttpRequestData carrier, string key)
            {
                return carrier.Headers.TryGetValues(key, out var values) ? new string[] {values.First()} : null;
            }
            transaction.AcceptDistributedTraceHeaders(req, Getter, NewRelic.Api.Agent.TransportType.HTTP);

            return transaction;
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
            NewRelic.Api.Agent.ITransaction transaction = helpNewRelic(req);
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            foreach(var header in req.Headers)
            {
                _logger.LogInformation(String.Format("Header {0}={1}", header.Key, String.Join(",", header.Value)));
            }

            this.generateError(transaction);
            this.waitSome(transaction);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }

