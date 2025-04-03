using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jbi.Worker;

/// <summary>
/// Defines an interface for a periodic worker that can be executed by a
/// background service on a recurring schedule.
/// </summary>
public interface IPeriodicWorker
{
	/// <summary>
	/// Executes a single period of the worker's task.
	/// </summary>
	/// <param name="cancellationToken">A token that indicates if the process should be cancelled.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// A background service that invokes functionality in periodic intervals.
/// Currently, we do not check for execution time. So when the actual work takes more time than the interval has, the work will continue.
/// </summary>
/// <typeparam name="TIteration">The type of the periodic worker, which must implement <see cref="IPeriodicWorker"/>.</typeparam>
/// <param name="scopeFactory">The <see cref="IServiceScopeFactory"/> used to create service scopes for each iteration.</param>
/// <param name="logger">The <see cref="ILogger{TIteration}"/> used for logging within the service.</param>
/// <param name="period">The time interval between the start of each iteration.</param>
/// <remarks>
/// This background service is responsible for executing a recurring task at a specified interval.
/// It leverages the <typeparamref name="TIteration"/> type, which is expected to implement the
/// <see cref="IPeriodicWorker"/> interface and contain the logic for the periodic work.
///
/// Each iteration of the worker is executed within its own service scope, ensuring that dependencies
/// are correctly resolved and managed for each execution cycle. This helps to prevent potential
/// state conflicts between different iterations.
///
/// **Important Note on Execution Time:**
/// The current implementation does not explicitly track the execution time of the periodic task.
/// If the time taken by the `RunAsync` method of the <typeparamref name="TIteration"/> exceeds the
/// specified <paramref name="period"/>, the next iteration will begin immediately after the previous
/// one completes. This means that the actual interval between the *end* of one execution and the
/// *start* of the next might be shorter than the configured <paramref name="period"/>. If precise
/// scheduling with fixed delays between the end of executions is required, a more sophisticated
/// implementation would be necessary.
/// </remarks>
public class PeriodicBackgroundService<TIteration>(
	IServiceScopeFactory scopeFactory,
	ILogger<TIteration> logger,
	TimeSpan period,
	TimeSpan initialDelay) 
	: BackgroundService
	where TIteration : IPeriodicWorker
{
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
	private readonly ILogger<TIteration> _logger = logger;
	private readonly TimeSpan _period = period;
	private readonly TimeSpan _initialDelay = initialDelay;

#if NET8_0_OR_GREATER
	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (_initialDelay > TimeSpan.Zero)
		{
			Log.InitialDelay(_logger, _initialDelay, nameof(TIteration));
			await Task.Delay(_initialDelay, stoppingToken).ConfigureAwait(false);
		}
		
		using PeriodicTimer timer = new (_period);
		Log.PeriodicWorkerStart(_logger, _period, nameof(TIteration));
		
		// The while loop will wait one period and then execute the loop.
		// A do while loop would run immediately and wait after the first iteration 
		while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
		{
			using var activity = Telemetry.StartActivity($"Periodic worker: {typeof(TIteration).Name}");
			var start = TimeProvider.System.GetTimestamp();
			
			try
			{
#pragma warning disable CA2007
				await using var scope = _scopeFactory.CreateAsyncScope();
#pragma warning restore CA2007
				var iteration = scope.ServiceProvider.GetRequiredService<TIteration>();

				Log.PeriodicWorkerIteration(_logger, nameof(TIteration));
				await iteration.RunAsync(stoppingToken).ConfigureAwait(false);
				var elapsedTime = TimeProvider.System.GetElapsedTime(start);
				Log.PeriodicWorkerIterationFinish(_logger, elapsedTime, nameof(TIteration));
			}
#pragma warning disable CA1031
			catch (Exception e)
#pragma warning restore CA1031
			{
				Log.PeriodicWorkerException(_logger, e, nameof(TIteration));
			}
		}

		Log.PeriodicWorkerStopped(_logger, nameof(TIteration));
	}
#else
	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (_initialDelay > TimeSpan.Zero)
		{
			Log.InitialDelay(_logger, _initialDelay, nameof(TIteration));
			await Task.Delay(_initialDelay, stoppingToken).ConfigureAwait(false);
		}

		Log.PeriodicWorkerStart(_logger, _period, nameof(TIteration));
		Stopwatch stopwatch = new ();
		
		while (!stoppingToken.IsCancellationRequested)
		{
			using var activity = Telemetry.StartActivity($"Periodic worker: {typeof(TIteration).Name}");
			stopwatch.Start();
			
			try
			{
#pragma warning disable CA2007
				await using var scope = _scopeFactory.CreateAsyncScope();
#pragma warning restore CA2007
				var iteration = scope.ServiceProvider.GetRequiredService<TIteration>();

				Log.PeriodicWorkerIteration(_logger, nameof(TIteration));
				var iterationTask = iteration.RunAsync(stoppingToken);
				var delayTask = Task.Delay(_period, stoppingToken);

				// Caution! We are starting the work and also the delay at the same time. 
				// The idea is that when the work takes longer than the delay, we will immediately execute
				// the next iteration
				await Task.WhenAll(iterationTask, delayTask).ConfigureAwait(false);
				stopwatch.Stop();
				Log.PeriodicWorkerIterationFinish(_logger, stopwatch.Elapsed, nameof(TIteration));
				stopwatch.Reset();
			}
#pragma warning disable CA1031
			catch (Exception e)
#pragma warning restore CA1031
			{
				Log.PeriodicWorkerException(_logger, e, nameof(TIteration));
			}

			Log.PeriodicWorkerStopped(_logger, nameof(TIteration));
		}
	}
#endif
	
}