using Xunit;

// Each test spins up its own in-process WebApplicationFactory/TestServer;
// running many of those concurrently across test classes caused rare
// malformed-response flakiness, so collections run sequentially instead.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
