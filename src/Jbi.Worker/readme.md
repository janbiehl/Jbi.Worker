# Jbi.Worker NuGet Package

This package provides base classes for implementing background services in .NET applications. It includes implementations for both continuous and periodic workers, simplifying the creation of long-running tasks.

## Overview

This package contains the following components:

* **`IContinuousWorker`**: An interface that defines the contract for a worker that runs continuously in a loop.
* **`ContinuousBackgroundService<TIteration>`**: An abstract base class for creating background services that execute a continuous worker (`TIteration`) in an infinite loop.
* **`IPeriodicWorker`**: An interface that defines the contract for a worker that executes on a recurring schedule.
* **`PeriodicBackgroundService<TIteration>`**: An abstract base class for creating background services that execute a periodic worker (`TIteration`) at a specified interval.

## Getting Started

### Installation

You can install the `Jbi.Worker` package using the NuGet Package Manager in Visual Studio or the .NET CLI:

```bash
dotnet add package Jbi.Worker
```

### Logging

Both background services use the standard ILogger<TIteration> interface for logging. Ensure that you have logging configured in your application to see the output from these services.

### Telemetry

The services also integrate with System.Diagnostics.Activity to create telemetry spans for each iteration, allowing for better monitoring and tracing of the background tasks.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for any bugs or enhancements.

## License

[MIT-License](https://choosealicense.com/licenses/mit/)