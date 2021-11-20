﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.SyncWork.Demo.Api.Services;
using Orleans.SyncWork.Demo.Api.Services.Grains;
using Orleans.SyncWork.Demo.Api.Services.TestGrains;
using Orleans.SyncWork.ExtensionMethods;

namespace Orleans.SyncWork.Demo.Api
{
    public static class OrleansConfigurationHelper
    {
        /// <summary>
        /// Starts a <see cref="ISiloHost"/> with default-ish options.
        /// </summary>
        /// <returns>The started <see cref="ISiloHost"/>.</returns>
        public static async Task<ISiloHost> StartSilo()
        {
            var builder = new SiloHostBuilder();
            ConfigureSiloHostBuilder(builder);

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        /// <summary>
        /// Configures the <see cref="ISiloHostBuilder"/> with a default amount of concurrent work (<see cref="Environment.ProcessorCount"/> - 2).
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>The <see cref="ISiloHostBuilder"/>.</returns>
        public static ISiloHostBuilder ConfigureSiloHostBuilder(this ISiloHostBuilder builder)
        {
            return builder.ConfigureSiloHostBuilder(Environment.ProcessorCount - 2);
        }

        /// <summary>
        /// Configures the <see cref="ISiloHostBuilder"/> with a specified amount of concurrent work.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="maxSyncWorkConcurrency">The maximum amount of concurrent work for each node of the cluster to take on at a time.</param>
        /// <returns>The <see cref="ISiloHostBuilder"/>.</returns>
        public static ISiloHostBuilder ConfigureSiloHostBuilder(this ISiloHostBuilder builder, int maxSyncWorkConcurrency)
        {
            builder
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IHelloWorld).Assembly).WithReferences())
                .ConfigureSyncWorkAbstraction(maxSyncWorkConcurrency)
                .ConfigureLogging(logging => logging.AddConsole())
                .UseDashboard(config =>
                {
                    config.Port = 8081;
                });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHelloWorld, HelloWorld>();
                services.AddSingleton<IPasswordVerifier, Services.PasswordVerifier>();
                services.AddSingleton<ISyncWorker<PasswordVerifierRequest, PasswordVerifierResult>, Services.Grains.PasswordVerifier>();
                services.AddSingleton<ISyncWorker<TestDelaySuccessRequest, TestDelaySuccessResult>, GrainThatWaitsSetTimePriorToSuccessResultBecomingAvailable>();
                services.AddSingleton<ISyncWorker<TestDelayExceptionRequest, TestDelayExceptionResult>, GrainThatWaitsSetTimePriorToExceptionResultBecomingAvailable>();
            });
            return builder;
        }
    }
}
