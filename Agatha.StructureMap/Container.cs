using System;
using Agatha.Common.InversionOfControl;
using Agatha.Common.WCF;
using sm = StructureMap;

namespace Agatha.StructureMap
{
    public class Container : Agatha.Common.InversionOfControl.IContainer
    {
        private readonly sm.IContainer _structureMapContainer;

        public Container() : this(new sm.Container()) { }

        public Container(sm.IContainer structureMapContainer)
        {
            _structureMapContainer = structureMapContainer;
        }

        public void Register(Type componentType, Type implementationType, Lifestyle lifeStyle)
        {
            _structureMapContainer.Configure(x => {
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
            if (implementationType == typeof(RequestProcessorProxy))
            {
                _structureMapContainer.Configure(x => x.ForConcreteType<RequestProcessorProxy>().Configure.SelectConstructor(() => new RequestProcessorProxy()));
            }

            if (implementationType == typeof(AsyncRequestProcessorProxy))
            {
                _structureMapContainer.Configure(x => x.ForConcreteType<AsyncRequestProcessorProxy>().Configure.SelectConstructor(() => new AsyncRequestProcessorProxy()));
            }
        }

        public void Register<TComponent, TImplementation>(Lifestyle lifestyle) where TImplementation : TComponent
        {
            Register(typeof(TComponent), typeof(TImplementation), lifestyle);
        }

        public void RegisterInstance(Type componentType, object instance)
        {
            _structureMapContainer.Configure(x => x.For(componentType).Use(instance));
        }

        public void RegisterInstance<TComponent>(TComponent instance) where TComponent : class
        {
            _structureMapContainer.Configure(x => x.For<TComponent>().Use(instance));
        }

        public TComponent Resolve<TComponent>()
        {
            return _structureMapContainer.GetInstance<TComponent>();
        }

        public TComponent Resolve<TComponent>(string key)
        {
            return _structureMapContainer.GetInstance<TComponent>(key);
        }

        public object Resolve(Type componentType)
        {
            return _structureMapContainer.GetInstance(componentType);
        }

        public TComponent TryResolve<TComponent>()
        {
            return _structureMapContainer.TryGetInstance<TComponent>();
        }

        public void Release(object component)
        {
            // NOTE: i think this needs to be a no-op in the case of structuremap... the code below was in the original patch but
            // i don't think its equivalent to Windsor's Release
            //structureMapContainer.Model.EjectAndRemove(component.GetType());
        }
    }
}
