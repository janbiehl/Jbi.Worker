using System.Diagnostics;

namespace Jbi.Worker;

public static class Telemetry
{
	public const string SourceName = "Jbi.Worker";
	public const string Version = "1.0.0";

	private static readonly ActivitySource ActivitySource = new (SourceName, Version);

	internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);
}