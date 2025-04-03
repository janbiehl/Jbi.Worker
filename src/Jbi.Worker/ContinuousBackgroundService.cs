using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jbi.Worker;

/// <summary>
/// Defines an interface for a continuous worker that can be executed by the
/// <see cref="ContinuousBackgroundService{TIteration}"/>.
/// </summary>
public interface IContinuousWorker
{
	/// <summary>
	/// Executes a single iteration of the continuous worker.
	/// </summary>
	/// <param name="cancellationToken">A token that indicates if the process should be cancelled.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// A base class for implementing continuously running background services.
/// Each iteration of the service is executed within its own service scope,
/// allowing for proper dependency management and disposal.
/// </summary>
/// <typeparam name="TIteration">
/// The type of the continuous worker that will be executed in each iteration.
/// This type must implement the <see cref="IContinuousWorker"/> interface.
/// </typeparam>
/// <remarks>
/// This class handles the common logic for starting, stopping, and iteratively
/// executing a background task. It also includes built-in logging and exception
/// handling for each iteration.
/// </remarks>
public class ContinuousBackgroundService<TIteration>(
	IServiceScopeFactory scopeFactory,
	ILogger<TIteration> logger,
	TimeSpan? initialDelay = null) 
	: BackgroundService
	where TIteration : IContinuousWorker
{
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
	private readonly ILogger<TIteration> _logger = logger;
	private readonly TimeSpan? _initialDelay = initialDelay;

	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (_initialDelay.HasValue && _initialDelay.Value > TimeSpan.Zero)
		{
			Log.InitialDelay(_logger, _initialDelay.Value, typeof(TIteration).Name);
			await Task.Delay(_initialDelay.Value, stoppingToken).ConfigureAwait(false);
		}
		
		Log.ContinuousWorkerStart(_logger, typeof(TIteration).Name);
		Stopwatch stopwatch = new ();
		
		while (!stoppingToken.IsCancellationRequested)
		{
			using var activity = Telemetry.StartActivity($"Continuous worker: {typeof(TIteration).Name}");
			stopwatch.Start();
			
			try
			{
#pragma warning disable CA2007
				await using var scope = _scopeFactory.CreateAsyncScope();
#pragma warning restore CA2007
				var iteration = scope.ServiceProvider.GetRequiredService<TIteration>();
				Log.ContinuousWorkerIteration(_logger, typeof(TIteration).Name);
				await iteration.RunAsync(stoppingToken).ConfigureAwait(false);
				stopwatch.Stop();
				Log.ContinuousWorkerIterationFinish(_logger, stopwatch.Elapsed, typeof(TIteration).Name);
				stopwatch.Reset();
			}
#pragma warning disable CA1031
			catch (Exception e)
#pragma warning restore CA1031
			{
				Log.ContinuousWorkerException(_logger, e, typeof(TIteration).Name);
			}
		}
		
		Log.ContinuousWorkerStopped(_logger, typeof(TIteration).Name);
	}
}