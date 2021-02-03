using System;
using Cassandra;
using Daemon.Configuration;
using Ductus.FluentDocker.Services;
using TestHarness.Compose;

namespace Daemon.IntegrationTests
{
    public class Cassandra : Dependency
    {
        private static Configuration.Cassandra _cassandra;
        public Cassandra() : base(("cassandra", (service, i) => CassandraProbe(i, service)))
        {
            _cassandra = Configuration.For<Configuration.Cassandra>();
        }
        private static Int32 CassandraProbe(Int32 i, IContainerService service)
        {
          Cluster cluster = default;
            ISession session = default;
            try
            {
                cluster = Cluster.Builder()
                    .AddContactPoints(_cassandra.Hosts)
                    .WithCredentials(_cassandra.UserName, _cassandra.Password)
                    .Build();
                session = cluster.Connect(PurchaseOrderHandler.KeySpace);
                session.Execute($"SELECT * FROM {PurchaseOrderHandler.TableName};").GetRows();
                return 0;
            }
            catch
            {
                return 500 /*ms*/;
            }
            finally
            {
                cluster?.Dispose();
                session?.Dispose();
            }
        }
    }
}
