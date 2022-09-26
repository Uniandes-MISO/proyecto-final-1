using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StravaPub
{
   public class Function : IHttpFunction
   {
       private readonly ILogger _logger;

       public Function(ILogger<Function> logger) =>
           _logger = logger;

        private NpgsqlConnection connection = null;
        private const string CONNECTION_STRING = "Host=34.123.164.10:5432;" + "Username=strava;" + "Password=aj2az6gbM3kuKb3n;" + "Database=strava";
        public readonly string ProjectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID") ?? "proyecto-grado-363519";
        public readonly string QueueId = Environment.GetEnvironmentVariable("GCP_QUEUE") ?? "GCP-QUEUE";
        public readonly string LocationId = Environment.GetEnvironmentVariable("LOCATION_ID") ?? "us-west1";

        public StravaModel Add(AuthModel data)
        {
            if (connection == null)
            {
                connection = new NpgsqlConnection(CONNECTION_STRING);
                connection.Open();
            }

            StravaModel send = new StravaModel();
            send.json = "{}";
            send.auth = data;
            send.IdUsuario = Guid.NewGuid().ToString();

            string commandText = $"INSERT INTO public.data (id_user, start_date, json) VALUES (@id, @start, @json)";
            using (var cmd = new NpgsqlCommand(commandText, connection))
            {
                cmd.Parameters.AddWithValue("id", send.IdUsuario);
                cmd.Parameters.AddWithValue("start", DateTime.Now );
                cmd.Parameters.AddWithValue("json", send.json);
                cmd.ExecuteNonQueryAsync();
            }

            var Url = $"https://strava-sub-kwefnjxija-uc.a.run.app/";
            var snippet = new CreateHttpTask();
            string str = JsonConvert.SerializeObject(send);

            _logger.LogError(str,"JSON");

            snippet.CreateTask(ProjectId, LocationId, QueueId, Url, str ,0);
                       
            return send;
        }

        public async Task HandleAsync(HttpContext context)
       {
           HttpRequest request = context.Request;

            AuthModel data = new AuthModel();

            data.client_id = ((string) request.Query["client_id"]);
            data.client_secret = ((string) request.Query["client_secret"]);
            data.grant_type = ((string)request.Query["grant_type"]);
            data.code = ((string)request.Query["code"]);

            if (string.IsNullOrEmpty(data.client_id) || string.IsNullOrEmpty(data.client_secret) || string.IsNullOrEmpty(data.grant_type) || string.IsNullOrEmpty(data.code))
            {
                await context.Response.WriteAsync($"Bad parameters");
            }

            var result = Add(data);

            var json_result = JsonConvert.SerializeObject(result);

            await context.Response.WriteAsync($"{json_result}");
       }
   }
}
