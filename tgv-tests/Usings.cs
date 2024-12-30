global using NUnit.Framework;
global using static tgv_tests.utils.AssertShortcuts;

[assembly: Parallelizable(ParallelScope.All)]
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]