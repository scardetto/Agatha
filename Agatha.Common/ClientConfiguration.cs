using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agatha.Common.Caching;
using Agatha.Common.Caching.Timers;
using Agatha.Common.InversionOfControl;
using Agatha.Common.WCF;

namespace Agatha.Common
{
	public class ClientConfiguration
	{
		private readonly List<Assembly> _requestsAndResponseAssemblies = new List<Assembly>();
		private readonly IContainer _container;

		public Type RequestDispatcherImplementation { get; set; }
		public Type RequestDispatcherFactoryImplementation { get; set; }
		public Type RequestProcessorImplementation { get; set; }
		public Type AsyncRequestDispatcherImplementation { get; set; }
		public Type AsyncRequestDispatcherFactoryImplementation { get; set; }
		public Type AsyncRequestProcessorImplementation { get; set; }
		public Type ContainerImplementation { get; }
		public Type CacheProviderImplementation { get; set; }
		public Type CacheManagerImplementation { get; set; }

		public ClientConfiguration(IContainer container)
		{
			_container = container;
			SetDefaultImplementations();
		}

		public ClientConfiguration(Type containerImplementation)
		{
			ContainerImplementation = containerImplementation;
			SetDefaultImplementations();
		}

		public ClientConfiguration(Assembly requestsAndResponsesAssembly, IContainer container)
			: this(container)
		{
			AddRequestAndResponseAssembly(requestsAndResponsesAssembly);
		}

		public ClientConfiguration(Assembly requestsAndResponsesAssembly, Type containerImplementation)
			: this(containerImplementation)
		{
			AddRequestAndResponseAssembly(requestsAndResponsesAssembly);
		}

		public ClientConfiguration AddRequestAndResponseAssembly(Assembly assembly)
		{
			_requestsAndResponseAssemblies.Add(assembly);
			return this;
		}

		private void SetDefaultImplementations()
		{
			RequestDispatcherImplementation = typeof(RequestDispatcher);
			RequestDispatcherFactoryImplementation = typeof(RequestDispatcherFactory);
			RequestProcessorImplementation = typeof(RequestProcessorProxy);
			CacheManagerImplementation = typeof(CacheManager);
			CacheProviderImplementation = typeof(InMemoryCacheProvider);
		}

		public void Initialize()
		{
			if (IoC.Container == null)
			{
				IoC.Container = _container ?? (IContainer)Activator.CreateInstance(ContainerImplementation);
			}

			IoC.Container.Register(typeof(IRequestProcessor), RequestProcessorImplementation, Lifestyle.Transient);
			IoC.Container.Register(typeof(IRequestDispatcher), RequestDispatcherImplementation, Lifestyle.Transient);
			IoC.Container.Register(typeof(IRequestDispatcherFactory), RequestDispatcherFactoryImplementation, Lifestyle.Singleton);
			IoC.Container.Register(typeof(ICacheProvider), CacheProviderImplementation, Lifestyle.Singleton);
			IoC.Container.Register(typeof(ICacheManager), CacheManagerImplementation, Lifestyle.Singleton);
			IoC.Container.Register<ITimerProvider, TimerProvider>(Lifestyle.Singleton);
			RegisterRequestAndResponseTypes();
			ConfigureCachingLayer();
		}

		private void ConfigureCachingLayer()
		{
			var requestTypes = _requestsAndResponseAssemblies.SelectMany(a => a.GetTypes()).Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Request)));
			var cacheConfiguration = new ClientCacheConfiguration(requestTypes);
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
	}
}