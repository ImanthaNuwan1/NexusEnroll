using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// Define Reverse Proxy Routes
var routes = new[]
{
    new RouteConfig
    {
        RouteId = "admin-route",
        ClusterId = "admin-cluster",
        Match = new RouteMatch { Path = "/api/admin/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "students-route",
        ClusterId = "students-cluster",
        Match = new RouteMatch { Path = "/api/students/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "faculty-route",
        ClusterId = "faculty-cluster",
        Match = new RouteMatch { Path = "/api/faculty/{**catch-all}" }
    }
};

// Define Reverse Proxy Target Clusters
var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "admin-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            { "admin-destination", new DestinationConfig { Address = "http://localhost:5030" } }
        }
    },
    new ClusterConfig
    {
        ClusterId = "students-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            { "students-destination", new DestinationConfig { Address = "http://localhost:5010" } }
        }
    },
    new ClusterConfig
    {
        ClusterId = "faculty-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            { "faculty-destination", new DestinationConfig { Address = "http://localhost:5020" } }
        }
    }
};

// Register YARP services
builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);

var app = builder.Build();

// Enable proxy routing middleware
app.MapReverseProxy();

// Run the Gateway on Port 5000
app.Run("http://localhost:5000");
