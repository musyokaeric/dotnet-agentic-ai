var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects._05_WebApi>("webapi")
    .WithUrls(context =>
    {
        var baseUrl = context.Urls.FirstOrDefault();
        if (baseUrl is not null)
        {
            context.Urls.Add(new()
            {
                Url = baseUrl.Url.TrimEnd('/') + "/devui",
                DisplayText = "DevUI Visual App"
            });
        }
    });

builder.Build().Run();
