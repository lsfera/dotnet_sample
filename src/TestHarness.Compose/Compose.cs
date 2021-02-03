using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ductus.FluentDocker.Builders;
using Xunit;

// ReSharper disable ClassNeverInstantiated.Global

namespace TestHarness.Compose
{
    public static class VisualStudioProvider
    {
        public static DirectoryInfo TryGetSolutionDirectoryInfo(String currentPath = null)
        {
            var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any()) directory = directory.Parent;
            return directory;
        }
    }

    public static class HostProvider
    {

        public static void EnforceEnvironmentVariables()
        {
            //When running outside CI we must provide the base path
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Environment.SetEnvironmentVariable("BASE_PATH", ".");
            //In CI we join an existent network - and we need to avoid orphan containers error detection on docker-compose down
            Environment.SetEnvironmentVariable("COMPOSE_IGNORE_ORPHANS", "true");

        }
    }

    public abstract class Compose
    {
        internal readonly CompositeBuilder ComposeBuilder;

        private Compose(params Dependency[] dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            HostProvider.EnforceEnvironmentVariables();
            var tuples = dependencies.Select(_ => (_.FileName, _.ReadinessProbe))
                .Select(_ =>
                {
                    var (fileName, (service, probe)) = _;

                    var composeFile = Path.IsPathRooted(fileName)
                        ? fileName
                        : Path.Combine(VisualStudioProvider.TryGetSolutionDirectoryInfo().FullName, fileName);
                    return (composeFile, service, probe);
                }).ToArray();
            var nw = new Builder().UseNetwork("demo").ReuseIfExist().Build();
            ComposeBuilder = new Builder()
                .UseContainer()
                .UseNetwork(nw)
                .UseCompose()
                .UseColor()
                .FromFile(tuples.Select(_ => _.composeFile).ToArray())
                .ForceRecreate();
            foreach (var (_, service, probe) in tuples)
                ComposeBuilder = ComposeBuilder.Wait(service, probe);
        }

        [CollectionDefinition(nameof(Collection), DisableParallelization = true)]
        public class Collection : ICollectionFixture<Compose>
        {
        }

        public class With<T> :
          Compose where T : Dependency, new()

        {
            public With()
              : base(new T())
            {
            }

        }

        public class With<T1, T2> :
          Compose
          where T1 : Dependency, new()
          where T2 : Dependency, new()

        {
            public With()
              : base(new T1(), new T2())
            {
            }

        }

        public class With<T1, T2, T3> :
          Compose
          where T1 : Dependency, new()
          where T2 : Dependency, new()
          where T3 : Dependency, new()

        {
            public With()
              : base(new T1(), new T2(), new T3())
            {
            }

        }

    }
}
