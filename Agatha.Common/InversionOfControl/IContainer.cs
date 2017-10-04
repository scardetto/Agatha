using System;

namespace Agatha.Common.InversionOfControl
{
    public interface IContainer : IDisposable
    {
        void Register(Type componentType, Type implementationType, Lifestyle lifeStyle);
        void Register<TComponent, TImplementation>(Lifestyle lifestyle) where TImplementation : TComponent;
        void RegisterInstance(Type componentType, object instance);
        void RegisterInstance<TComponent>(TComponent instance) where TComponent : class;

        TComponent Resolve<TComponent>();
        TComponent Resolve<TComponent>(string key);
        object Resolve(Type componentType);
        TComponent TryResolve<TComponent>();

        IContainer GetChildContainer();
    }
}