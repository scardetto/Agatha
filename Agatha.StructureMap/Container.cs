using System;
using Agatha.Common.InversionOfControl;
using Agatha.Common.WCF;
using sm = StructureMap;

namespace Agatha.StructureMap
{
    public class Container : IContainer
    {
        private readonly sm.IContainer _container;

        public Container() : this(new sm.Container()) { }

        public Container(sm.IContainer container)
        {
            _container = container;
        }

        public void Register(Type componentType, Type implementationType, Lifestyle lifeStyle)
        {
            _container.Configure(x => {
                if (lifeStyle == Lifestyle.Singleton) {
                    x.ForSingletonOf(componentType).Use(implementationType);
                } else {
                    x.For(componentType).Use(implementationType);
                }
            });

            OverrideConstructorResolvingWhenUsingRequestProcessorProxy(implementationType);
        }

        private void OverrideConstructorResolvingWhenUsingRequestProcessorProxy(Type implementationType)
        {
            if (implementationType == typeof(RequestProcessorProxy)) {
                _container.Configure(x => x.ForConcreteType<RequestProcessorProxy>().Configure.SelectConstructor(() => new RequestProcessorProxy()));
            }

            if (implementationType == typeof(AsyncRequestProcessorProxy)) {
                _container.Configure(x => x.ForConcreteType<AsyncRequestProcessorProxy>().Configure.SelectConstructor(() => new AsyncRequestProcessorProxy()));
            }
        }

        public void Register<TComponent, TImplementation>(Lifestyle lifestyle) where TImplementation : TComponent
        {
            Register(typeof(TComponent), typeof(TImplementation), lifestyle);
        }

        public void RegisterInstance(Type componentType, object instance)
        {
            _container.Configure(x => x.For(componentType).Use(instance));
        }

        public void RegisterInstance<TComponent>(TComponent instance) where TComponent : class
        {
            _container.Configure(x => x.For<TComponent>().Use(instance));
        }

        public TComponent Resolve<TComponent>()
        {
            return _container.GetInstance<TComponent>();
        }

        public TComponent Resolve<TComponent>(string key)
        {
            return _container.GetInstance<TComponent>(key);
        }

        public object Resolve(Type componentType)
        {
            return _container.GetInstance(componentType);
        }

        public TComponent TryResolve<TComponent>()
        {
            return _container.TryGetInstance<TComponent>();
        }

        public IContainer GetChildContainer()
        {
            return new Container(_container.CreateChildContainer());
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
