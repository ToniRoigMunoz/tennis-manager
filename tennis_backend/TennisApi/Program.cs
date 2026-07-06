using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var conn = Environment.GetEnvironmentVariable("CosmosDBConnection")!;
        
        // Configuramos Cosmos DB para que serialice usando camelCase
        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };
        
        services.AddSingleton(_ => new CosmosClient(conn, options));
    })
    .Build();

host.Run();