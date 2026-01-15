using Xunit;

// Disable parallel test execution to prevent race conditions on static plugin properties
[assembly: CollectionBehavior(DisableTestParallelization = true)]
