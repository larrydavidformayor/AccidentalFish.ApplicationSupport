﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccidentalFish.ApplicationSupport.Core.Components;
using AccidentalFish.ApplicationSupport.Core.Logging;
using Microsoft.Practices.Unity;

namespace AccidentalFish.ApplicationSupport.Core.Runtime.Implementation
{
    [ComponentIdentity(FullyQualifiedName)]
    internal class ComponentHost : AbstractApplicationComponent, IComponentHost
    {
        public const string FullyQualifiedName = "com.accidentalfish.application-support.component-host";
        private readonly IUnityContainer _unityContainer;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;

        public ComponentHost(IUnityContainer unityContainer, ILoggerFactory loggerFactory)
        {
            _unityContainer = unityContainer;
            _logger = loggerFactory.CreateLongLivedLogger(ComponentIdentity);
        }

        public async Task<IEnumerable<Task>> Start(IComponentHostConfigurationProvider configurationProvider, CancellationTokenSource cancellationTokenSource)
        {
            IEnumerable<ComponentConfiguration> componentConfigurations = await configurationProvider.GetConfiguration();
            _cancellationTokenSource = cancellationTokenSource;
            List<Task> tasks = new List<Task>();
            foreach (ComponentConfiguration componentConfiguration in componentConfigurations)
            {
                _logger.Information(String.Format("Starting {0} instances of {1}", componentConfiguration.Instances, componentConfiguration.ComponentIdentity));
                for (int instance = 0; instance < componentConfiguration.Instances; instance++)
                {
                    tasks.Add(StartTask(componentConfiguration.ComponentIdentity, componentConfiguration.RestartEvaluator));
                }
            }
            return tasks;
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        // TODO: Needs attention as this isn't right yet - too tired!
        private Task StartTask(IComponentIdentity componentIdentity, Func<Exception, int, bool> restartEvaluator)
        {
            return Task.Factory.StartNew(() =>
            {
                int retryCount = 0;
                bool shouldRetry = true;
                while (shouldRetry)
                {
                    try
                    {
                        Task.Factory.StartNew(() =>
                        {
                            _logger.Information(String.Format("Hostable component {0} is starting", componentIdentity));
                            IHostableComponent component = _unityContainer.Resolve<IHostableComponent>(componentIdentity.ToString());
                            component.Start(_cancellationTokenSource.Token).Wait();
                            shouldRetry = false; // normal exit
                            _logger.Information(String.Format("Hostable component {0} is exiting", componentIdentity));
                        }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Wait();
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        shouldRetry = restartEvaluator != null && restartEvaluator(ex, retryCount);
                        if (shouldRetry)
                        {
                            _logger.Information(String.Format("Restarting {0} for component {1}", retryCount, componentIdentity));
                        }
                        else
                        {
                            _logger.Error(String.Format("Component failure {0} for component {1}", retryCount, componentIdentity));
                        }
                    }
                }
                
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
