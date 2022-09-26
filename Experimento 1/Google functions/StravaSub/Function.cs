using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StravaSub
{
    //https://www.strava.com/oauth/authorize?client_id=94332&response_type=code&redirect_uri=http://localhost:8080&approval_prompt=force&scope=activity:write,read_all
    
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


        public void Update(StravaModel data)
        {
            if (connection == null)
            {
                connection = new NpgsqlConnection(CONNECTION_STRING);
                connection.Open();
            }

            var commandText = $@"UPDATE public.data 
                SET end_date = @end, json = @js
                WHERE id_user = @id";

            using (var cmd = new NpgsqlCommand(commandText, connection))
            {

                cmd.Parameters.AddWithValue("id", data.IdUsuario);
                //cmd.Parameters.AddWithValue("start", DateTime.Now );
                cmd.Parameters.AddWithValue("end", DateTime.Now);
                cmd.Parameters.AddWithValue("js", data.json);

                cmd.ExecuteNonQueryAsync();
            }
        }
        
        public async Task HandleAsync(HttpContext context)
       {
            HttpRequest request = context.Request;

            // If there's a body, parse it as JSON and check for "name" field.
            using TextReader reader = new StreamReader(request.Body);
            string data = await reader.ReadToEndAsync();

            // Log the request payload
            var task = JsonConvert.DeserializeObject<StravaModel>(data);
            task.json = $"{task.IdUsuario}";
            _logger.LogDebug(data, "JSON");

            Update(task);

            await context.Response.WriteAsync($"Result: {data}");
       }
   }
}
