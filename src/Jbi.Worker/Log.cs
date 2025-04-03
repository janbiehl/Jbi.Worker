using Microsoft.Extensions.Logging;

namespace Jbi.Worker;

internal static partial class Log
{
	[LoggerMessage(LogLevel.Debug, "Initial delay of {Delay} for worker of type {IterationTypeName}")]
	internal static partial void InitialDelay(ILogger logger, TimeSpan delay, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Information, "Periodic worker of type {IterationTypeName} starting with period {Period}")]
	internal static partial void PeriodicWorkerStart(ILogger logger, TimeSpan period, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Debug, "Starting iteration of {IterationTypeName}")]
	internal static partial void PeriodicWorkerIteration(ILogger logger, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Debug, "Finished iteration of {IterationTypeName} in {Duration}")]
	internal static partial void PeriodicWorkerIterationFinish(ILogger logger, TimeSpan duration, string iterationTypeName);

	[LoggerMessage(LogLevel.Information, "Periodic worker of type {IterationTypeName} stopped")]
	internal static partial void PeriodicWorkerStopped(ILogger logger, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Error, "An exception occurred during periodic worker execution: {IterationTypeName}")]
	internal static partial void PeriodicWorkerException(ILogger logger, Exception exception, string iterationTypeName);
	
	
	[LoggerMessage(LogLevel.Information, "Continuous worker of type {IterationTypeName} starting")]
	internal static partial void ContinuousWorkerStart(ILogger logger, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Debug, "Starting iteration of {IterationTypeName}")]
	internal static partial void ContinuousWorkerIteration(ILogger logger, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Debug, "Finished iteration of {IterationTypeName} in {Duration}")]
	internal static partial void ContinuousWorkerIterationFinish(ILogger logger, TimeSpan duration, string iterationTypeName);

	[LoggerMessage(LogLevel.Information, "Continuous worker of type {IterationTypeName} stopped")]
	internal static partial void ContinuousWorkerStopped(ILogger logger, string iterationTypeName);
	
	[LoggerMessage(LogLevel.Error, "An exception occurred during continuous worker execution: {IterationTypeName}")]
	internal static partial void ContinuousWorkerException(ILogger logger, Exception exception, string iterationTypeName);

}