using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jbi.Worker;

public static class Registration
{
	/// <summary>
	/// Adds a periodic background worker to the service collection.
	/// </summary>
	/// <typeparam name="TIteration">The type of the periodic worker, which must implement <see cref="IPeriodicWorker"/>.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the worker to.</param>
	/// <param name="period">The time interval between iterations of the worker.</param>
	/// <param name="initialDelay">Delays the first execution</param>
	/// <returns>The modified <see cref="IServiceCollection"/>.</returns>
	/// <remarks>
	/// This method registers the specified <typeparamref name="TIteration"/> type as a scoped service
	/// and then registers a <see cref="PeriodicBackgroundService{TIteration}"/> as a hosted service.
	/// The <see cref="PeriodicBackgroundService{TIteration}"/> will use the provided <paramref name="period"/>
	/// to execute the logic defined in the <typeparamref name="TIteration"/> implementation of the
	/// <see cref="IPeriodicWorker"/> interface. Each iteration will occur within its own service scope,
	/// ensuring dependencies are properly managed.
	/// </remarks>
	/// <example>
	/// To register a periodic worker named `MyPeriodicTask` that runs every 5 minutes:
	/// <code>
	/// public class MyPeriodicTask : IPeriodicWorker
	/// {
	///     private readonly ILogger&lt;MyPeriodicTask&gt; _logger;
	/// 
	///     public MyPeriodicTask(ILogger&lt;MyPeriodicTask&gt; logger)
	///     {
	///         _logger = logger;
	///     }
	/// 
	///     public async Task DoWorkAsync(CancellationToken stoppingToken)
	///     {
	///         _logger.LogInformation("MyPeriodicTask is running at: {time}", DateTimeOffset.Now);
	///         await Task.Delay(1000, stoppingToken); // Simulate some work
	///     }
	/// }
	/// 
	/// public static class Program
	/// {
	///     public static async Task Main(string[] args)
	///     {
	///         var host = Host.CreateDefaultBuilder(args)
	///             .ConfigureServices(services =>
	///             {
	///                 services.AddPeriodicWorker&lt;MyPeriodicTask&gt;(TimeSpan.FromMinutes(5));
	///             })
	///             .Build();
	/// 
	///         await host.RunAsync();
	///     }
	/// }
	/// </code>
	/// </example>
	public static IServiceCollection AddPeriodicWorker<TIteration>(this IServiceCollection services, TimeSpan period, TimeSpan? initialDelay)
		where TIteration : class, IPeriodicWorker
	{
		services.AddScoped<TIteration>();
		services.AddHostedService<PeriodicBackgroundService<TIteration>>(sp =>
		{
			var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
			var logger = sp.GetRequiredService<ILogger<TIteration>>();
			return new PeriodicBackgroundService<TIteration>(scopeFactory, logger, period, initialDelay ?? TimeSpan.Zero);
		});
		return services;
	}

	/// <summary>
	/// Adds a continuous background worker to the service collection.
	/// </summary>
	/// <typeparam name="TIteration">The type of the continuous worker, which must implement <see cref="IContinuousWorker"/>.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the worker to.</param>
	/// <param name="initialDelay">Delay the first execution for the worker</param>
	/// <returns>The modified <see cref="IServiceCollection"/>.</returns>
	/// <remarks>
	/// This method registers the specified <typeparamref name="TIteration"/> type as a scoped service
	/// and then registers a <see cref="ContinuousBackgroundService{TIteration}"/> as a hosted service.
	/// The <see cref="ContinuousBackgroundService{TIteration}"/> will execute the logic defined in the
	/// <typeparamref name="TIteration"/> implementation of the <see cref="IContinuousWorker"/> interface
	/// in a continuous loop until the application shuts down. Each iteration will occur within its own
	/// service scope, ensuring dependencies are properly managed.
	/// </remarks>
	/// <example>
	/// To register a continuous worker named `MyContinuousTask`:
	/// <code>
	/// public class MyContinuousTask : IContinuousWorker
	/// {
	///     private readonly ILogger&lt;MyContinuousTask&gt; _logger;
	/// 
	///     public MyContinuousTask(ILogger&lt;MyContinuousTask&gt; logger)
	///     {
	///         _logger = logger;
	///     }
	/// 
	///     public async Task DoWorkAsync(CancellationToken stoppingToken)
	///     {
	///         while (!stoppingToken.IsCancellationRequested)
	///         {
	///             _logger.LogInformation("MyContinuousTask is running at: {time}", DateTimeOffset.Now);
	///             await Task.Delay(1000, stoppingToken); // Simulate some continuous work
	///         }
	///     }
	/// }
	/// 
	/// public static class Program
	/// {
	///     public static async Task Main(string[] args)
	///     {
	///         var host = Host.CreateDefaultBuilder(args)
	///             .ConfigureServices(services =>
	///             {
	///                 services.AddContinuousWorker&lt;MyContinuousTask&gt;();
	///             })
	///             .Build();
	/// 
	///         await host.RunAsync();
	///     }
	/// }
	/// </code>
	/// </example>
	public static IServiceCollection AddContinuousWorker<TIteration>(this IServiceCollection services, TimeSpan? initialDelay) 
		where TIteration : class, IContinuousWorker
	{
		services.AddScoped<TIteration>();
		services.AddHostedService<ContinuousBackgroundService<TIteration>>(sp =>
		{
			var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
			var logger = sp.GetRequiredService<ILogger<TIteration>>();
			return new ContinuousBackgroundService<TIteration>(scopeFactory, logger, initialDelay ?? TimeSpan.Zero);
		});
		return services;
	}
}