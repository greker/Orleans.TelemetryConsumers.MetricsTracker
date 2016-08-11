using System;
using System.Threading.Tasks;
using System.Threading;
using Orleans;
using Orleans.Runtime.Configuration;
using Orleans.TelemetryConsumers.MetricsTracker;

namespace Orleans.TelemetryConsumers.MetricsTracker.TestHost
{
    public class Program
    {
        static void Main(string[] args)
        {
            var SyncContext = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(SyncContext);

            // The Orleans silo environment is initialized in its own app domain in order to more
            // closely emulate the distributed situation, when the client and the server cannot
            // pass data via shared memory.
            AppDomain hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                AppDomainInitializerArguments = args,
            });

            var config = ClientConfiguration.LocalhostSilo();
            GrainClient.Initialize(config);

            // TODO: once the previous call returns, the silo is up and running.
            //       This is the place your custom logic, for example calling client logic
            //       or initializing an HTTP front end for accepting incoming requests.

            var metricsGrain = GrainClient.GrainFactory.GetGrain<IClusterMetricsGrain>(Guid.Empty);
            metricsGrain.ReportSiloStatistics(new MetricsSnapshot()).Wait();

            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();

            hostDomain.DoCallBack(ShutdownSilo);
        }

        static void InitSilo(string[] args)
        {
            var SyncContext = SynchronizationContext.Current;
            if (SyncContext == null)
            {
                SyncContext = new SynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(SyncContext);
            }

            MetricsTrackerTelemetryConsumer.SyncContext = SyncContext;

            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
                Console.Error.WriteLine("Failed to initialize Orleans silo");
        }

        static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static OrleansHostWrapper hostWrapper;
    }
}