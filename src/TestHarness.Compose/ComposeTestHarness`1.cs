using Ductus.FluentDocker.Services;
using Xunit;
using Xunit.Abstractions;

namespace TestHarness.Compose
{
  public abstract class ComposeTestHarness<T> :
    TestHarnessWithLog,
    IClassFixture<Compose.With<T>> where T : Dependency, new()
  {
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly ICompositeService ComposeService;

    protected ComposeTestHarness(ITestOutputHelper testOutputHelper, Compose.With<T> fixture)
      : base(testOutputHelper) =>
      ComposeService = fixture.ComposeBuilder.Build().Start();

    public override void Dispose()
    {
      ComposeService?.Dispose();
      base.Dispose();
    }
  }
}
