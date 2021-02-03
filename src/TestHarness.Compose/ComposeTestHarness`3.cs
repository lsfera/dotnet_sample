using Ductus.FluentDocker.Services;
using Xunit;
using Xunit.Abstractions;

namespace TestHarness.Compose
{
  public abstract class ComposeTestHarness<T1, T2, T3> :
    TestHarnessWithLog,
    IClassFixture<Compose.With<T1, T2, T3>> where T1 : Dependency, new()
                                            where T2 : Dependency, new()
                                            where T3 : Dependency, new()
  {
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly ICompositeService ComposeService;

    protected ComposeTestHarness(ITestOutputHelper testOutputHelper, Compose.With<T1, T2, T3> fixture)
      : base(testOutputHelper) =>
      ComposeService = fixture.ComposeBuilder.Build().Start();

    public override void Dispose()
    {
      ComposeService?.Dispose();
      base.Dispose();
    }
  }
}
