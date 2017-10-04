using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agatha.Common;
using Agatha.Common.Caching;
using Agatha.Common.Caching.Timers;
using Agatha.Common.Configuration;
using Agatha.Common.InversionOfControl;
using Agatha.Common.WCF;

namespace Agatha.ServiceLayer
{
    public class ServiceLayerConfiguration
    {
        private readonly List<Assembly> _requestHandlerAssemblies = new List<Assembly>();
        private readonly List<Assembly> _requestsAndResponseAssemblies = new List<Assembly>();
        private readonly IContainer _container;
        private readonly List<Type> _registeredInterceptors = new List<Type>();

        public Type RequestProcessorImplementation { get; set; }
        public Type AsyncRequestProcessorImplementation { get; set; }
        public Type CacheManagerImplementation { get; set; }
        public Type CacheProviderImplementation { get; set; }
        public Type ContainerImplementation { get; }

        public IRequestTypeRegistry RequestTypeRegistry { get; private set; }
        public IRequestHandlerRegistry RequestHandlerRegistry { get; private set; }
        public Type BusinessExceptionType { get; set; }
        public Type SecurityExceptionType { get; set; }
        public Type Conventions { get; private set; }

        public ServiceLayerConfiguration(IContainer container)
        {
            _container = container;
            SetDefaultImplementations();
        }

        public ServiceLayerConfiguration(Type containerImplementation)
        {
            ContainerImplementation = containerImplementation;
            SetDefaultImplementations();
        }

        public ServiceLayerConfiguration(Assembly requestHandlersAssembly, Assembly requestsAndResponsesAssembly, IContainer container)
            : this(container)
        {
            AddRequestHandlerAssembly(requestHandlersAssembly);
            AddRequestAndResponseAssembly(requestsAndResponsesAssembly);
        }

        public ServiceLayerConfiguration(Assembly requestHandlersAssembly, Assembly requestsAndResponsesAssembly, Type containerImplementation)
            : this(containerImplementation)
        {
            AddRequestHandlerAssembly(requestHandlersAssembly);
            AddRequestAndResponseAssembly(requestsAndResponsesAssembly);
        }

        public ServiceLayerConfiguration AddRequestHandlerAssembly(Assembly assembly)
        {
            _requestHandlerAssemblies.Add(assembly);
            return this;
        }

        public ServiceLayerConfiguration AddRequestAndResponseAssembly(Assembly assembly)
        {
            _requestsAndResponseAssemblies.Add(assembly);
            return this;
        }

        private void SetDefaultImplementations()
        {
            RequestProcessorImplementation = typeof(RequestProcessor);
            CacheManagerImplementation = typeof(CacheManager);
            CacheProviderImplementation = typeof(InMemoryCacheProvider);
            RequestTypeRegistry = new WcfKnownTypesBasedRequestTypeRegistry();
            RequestHandlerRegistry = new RequestHandlerRegistry();
            RegisterRequestHandlerInterceptor<CachingInterceptor>();
        }

        public void Initialize()
        {
            if (IoC.Container == null)
            {
                IoC.Container = _container ?? (IContainer)Activator.CreateInstance(ContainerImplementation);
            }

            IoC.Container.RegisterInstance(this);
            IoC.Container.RegisterInstance(RequestTypeRegistry);
            IoC.Container.RegisterInstance(RequestHandlerRegistry);
            IoC.Container.Register(typeof(IRequestProcessor), RequestProcessorImplementation, Lifestyle.Transient);
            IoC.Container.Register(typeof(ICacheProvider), CacheProviderImplementation, Lifestyle.Singleton);
            IoC.Container.Register(typeof(ICacheManager), CacheManagerImplementation, Lifestyle.Singleton);
            IoC.Container.Register<ITimerProvider, TimerProvider>(Lifestyle.Singleton);
            if (Conventions != null) IoC.Container.Register(typeof(IConventions), Conventions, Lifestyle.Singleton);
            IoC.Container.Register<IRequestProcessingErrorHandler, RequestProcessingErrorHandler>(Lifestyle.Transient);
            RegisterRequestAndResponseTypes();
            RegisterRequestHandlers();
            ConfigureCachingLayer();
            RegisterInterceptors();
        }

        private void RegisterInterceptors()
        {
            foreach (var interceptorType in _registeredInterceptors)
            {
                IoC.Container.Register(interceptorType, interceptorType, Lifestyle.Transient);
            }
        }

        private void ConfigureCachingLayer()
        {
            var requestTypes = _requestsAndResponseAssemblies.SelectMany(a => a.GetTypes()).Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Request)));
            var cacheConfiguration = new ServiceCacheConfiguration(requestTypes);
            IoC.Container.RegisterInstance<CacheConfiguration>(cacheConfiguration);
        }

        private void RegisterRequestAndResponseTypes()
        {
            foreach (var assembly in _requestsAndResponseAssemblies)
            {
                KnownTypeProvider.RegisterDerivedTypesOf<Request>(assembly);
                KnownTypeProvider.RegisterDerivedTypesOf<Response>(assembly);
            }
        }

        private void RegisterRequestHandlers()
        {
            var requestWithRequestHandlers = new Dictionary<Type, Type>();

            foreach (var assembly in _requestHandlerAssemblies) {
                foreach (var type in assembly.GetTypes()) {
                    if (type.IsAbstract || type.IsGenericType)
                        continue;

                    RequestHandlerRegistry.Register(type);

                    var requestType = GetRequestType(type);

                    if (requestType == null) continue;
                    var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

                    if (requestWithRequestHandlers.ContainsKey(requestType)) {
                        throw new InvalidOperationException(
                            $"Found two request handlers that handle the same request: {requestType.FullName}. " +
                            $" First request handler: {type.FullName}, second: {requestWithRequestHandlers[requestType].FullName}. " +
                            " For each request type there must by only one request handler.");
                    }

                    IoC.Container.Register(handlerType, type, Lifestyle.Transient);
                    requestWithRequestHandlers.Add(requestType, type);
                }
            }
        }

        private static Type GetRequestType(Type type)
        {
            var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("IRequestHandler`") || i.Name.StartsWith("IOneWayRequestHandler`"));

            if (interfaceType == null || interfaceType.GetGenericArguments().Length == 0) {
                return null;
            }

            return GetFirstGenericTypeArgument(interfaceType);
        }

        private static Type GetFirstGenericTypeArgument(Type type)
        {
            return type.GetGenericArguments()[0];
        }

        public ServiceLayerConfiguration RegisterRequestHandlerInterceptor<T>() where T : IRequestHandlerInterceptor
        {
            _registeredInterceptors.Add(typeof(T));
            return this;
        }

        public ServiceLayerConfiguration Use<TConventions>() where TConventions : IConventions
        {
            Conventions = typeof(TConventions);
            return this;
        }

        public IList<Type> GetRegisteredInterceptorTypes()
        {
            return _registeredInterceptors;
        }
    }
}