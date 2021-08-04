# DripFeed
A simple, fluent ASP.NET 5.0 library to throttle your requests!

## Examples

### Single action in controller

```csharp
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {

        services.AddControllers();

        //Configure the controller and actions here
        services.AddDripFeed(options =>
        {
            options.UseMemoryCache();
            options.AddController<WeatherForecastController>(controller =>
            {
                controller.AddAction(a => a.Get);
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();


        //Inject the middleware here
        app.UseDripFeed();




        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

```

### Multiple actions in controller

```csharp

public void ConfigureServices(IServiceCollection services)
{

    services.AddControllers();

    //Configure the controller and actions here
    services.AddDripFeed(options =>
    {
        options.UseMemoryCache();
        options.AddController<WeatherForecastController>(controller =>
        {
            controller.AddAction(a => a.Get).WithLimit(TimeSpan.FromMinutes(1), 300);;
            controller.AddAction(a => a.GetOther).WithLimit(TimeSpan.FromMinutes(5), 500);;
        });
    });
}


```

### All actions in a controller

```csharp
public void ConfigureServices(IServiceCollection services)
{

    services.AddControllers();
    //Configure the controller and actions here
    services.AddDripFeed(options =>
    {
        options.UseMemoryCache();
        options.AddAllActionsFromController<WeatherForecastController>(TimeSpan.FromSeconds(30), 100);
    });
}
```

### All actions in a controller and some with a different rate limiting

```csharp
public void ConfigureServices(IServiceCollection services)
{

    services.AddControllers();
    //Configure the controller and actions here
    services.AddDripFeed(options =>
    {
        options.UseMemoryCache();
        options.AddAllActionsFromController<WeatherForecastController>(TimeSpan.FromSeconds(30), 100);
        options.AddController<WeatherForecastController>(c =>
        {
            c.AddAction(a => a.Get).WithLimit(TimeSpan.FromMinutes(1), 300);
        });
    });
}
```

### All options

```csharp
public void ConfigureServices(IServiceCollection services)
{

    services.AddControllers();
    services.AddDripFeed(options =>
    {
        //use either one of these. You need to add IMemoryCache of IDistributedCache to your DI container respectively!
        options.UseMemoryCache(); //Use IMemoryCache to check for throttling
        options.UseDistributedCache(); //Use IDistributedCache to check for throttling (USE THIS IN PRODUCTION)

        //Use a custom identifier for the request. Defaults to the remote IP address
        options.UseCustomIdentifier(httpContext => httpContext.Request.Headers["X-Real-IP"]); 
        options.UseStatusCode(418); //Use a custom status code. Defaults to 429 Too Many Requests
        
        options.AddAllActionsFromController<WeatherForecastController>(TimeSpan.FromSeconds(30), 100);
        options.AddController<WeatherForecastController>(c =>
        {
            c.AddAction(a => a.Get).WithLimit(TimeSpan.FromMinutes(1), 300);

        });
    });
}
```
