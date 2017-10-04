using System;
using System.Collections.Generic;
using System.Reflection;
using Agatha.Common;
using Agatha.Common.Caching;
using Agatha.Common.InversionOfControl;

namespace Agatha.ServiceLayer
{
    public class ServiceLayerAndClientConfiguration
    {
        private readonly List<Assembly> _requestHandlerAssemblies = new List<Assembly>();
        private readonly List<Assembly> _requestsAndResponseAssemblies = new List<Assembly>();
        private readonly IContainer _container;
        private ServiceLayerConfiguration _serviceLayerConfiguration;

        public Type RequestProcessorImplementation { get; set; }
        public Type AsyncRequestProcessorImplementation { get; set; }
        public Type CacheManagerImplementation { get; set; }
        public Type CacheProviderImplementation { get; set; }
        public Type ContainerImplementation { get; }
        public Type BusinessExceptionType { get; set; }
        public Type SecurityExceptionType { get; set; }

        public Type RequestDispatcherImplementation { get; set; }
        public Type RequestDispatcherFactoryImplementation { get; set; }
        public Type AsyncRequestDispatcherImplementation { get; set; }
        public Type AsyncRequestDispatcherFactoryImplementation { get; set; }

        public ServiceLayerAndClientConfiguration(IContainer container)
        {
            _container = container;
            SetDefaultImplementations();
        }

        public ServiceLayerAndClientConfiguration(Type containerImplementation)
        {
            ContainerImplementation = containerImplementation;
            SetDefaultImplementations();
        }

        public ServiceLayerAndClientConfiguration(Assembly requestHandlersAssembly, Assembly requestsAndResponsesAssembly, IContainer container)
            : this(container)
        {
            AddRequestHandlerAssembly(requestHandlersAssembly);
            AddRequestAndResponseAssembly(requestsAndResponsesAssembly);
        }

        public ServiceLayerAndClientConfiguration(Assembly requestHandlersAssembly, Assembly requestsAndResponsesAssembly, Type containerImplementation)
            : this(containerImplementation)
        {
            AddRequestHandlerAssembly(requestHandlersAssembly);
            AddRequestAndResponseAssembly(requestsAndResponsesAssembly);
        }

        public ServiceLayerAndClientConfiguration AddRequestHandlerAssembly(Assembly assembly)
        {
            _requestHandlerAssemblies.Add(assembly);
            return this;
        }

        public ServiceLayerAndClientConfiguration AddRequestAndResponseAssembly(Assembly assembly)
        {
            _requestsAndResponseAssemblies.Add(assembly);
            return this;
        }

        private void SetDefaultImplementations()
        {
            RequestDispatcherImplementation = typeof(RequestDispatcher);
            RequestDispatcherFactoryImplementation = typeof(RequestDispatcherFactory);
            RequestProcessorImplementation = typeof(RequestProcessor);
            CacheManagerImplementation = typeof(CacheManager);
            CacheProviderImplementation = typeof(InMemoryCacheProvider);

            IoC.Container = _container ?? (IContainer)Activator.CreateInstance(ContainerImplementation);
            _serviceLayerConfiguration = new ServiceLayerConfiguration(IoC.Container);
        }

        public void Initialize()
        {
            _serviceLayerConfiguration.AsyncRequestProcessorImplementation = AsyncRequestProcessorImplementation;
            _serviceLayerConfiguration.BusinessExceptionType = BusinessExceptionType;
            _serviceLayerConfiguration.RequestProcessorImplementation = RequestProcessorImplementation;
            _serviceLayerConfiguration.SecurityExceptionType = SecurityExceptionType;
            _serviceLayerConfiguration.CacheManagerImplementation = CacheManagerImplementation;
            _serviceLayerConfiguration.CacheProviderImplementation = CacheProviderImplementation;

            foreach (var assembly in _requestHandlerAssemblies)
                _serviceLayerConfiguration.AddRequestHandlerAssembly(assembly);

            foreach (var assembly in _requestsAndResponseAssemblies)
                _serviceLayerConfiguration.AddRequestAndResponseAssembly(assembly);

            _serviceLayerConfiguration.Initialize();

            IoC.Container.Register(typeof(IRequestDispatcher), RequestDispatcherImplementation, Lifestyle.Transient);
            IoC.Container.Register(typeof(IRequestDispatcherFactory), RequestDispatcherFactoryImplementation, Lifestyle.Singleton);
        }

         public ServiceLayerAndClientConfiguration RegisterRequestHandlerInterceptor<T>() where T : IRequestHandlerInterceptor
         {
             _serviceLayerConfiguration.RegisterRequestHandlerInterceptor<T>();
             return this;
         }

         public ServiceLayerAndClientConfiguration Use<TConventions>() where TConventions : IConventions
         {
             _serviceLayerConfiguration.Use<TConventions>();
             return this;
         }
    }
}