﻿using DryIoc;
using EPiServer.ServiceLocation;
using System;

namespace DryIocEpi
{
    public class DryIocServiceConfigurationProvider : IServiceConfigurationProvider, IRegisteredService, EPiServer.ServiceLocation.Internal.IInterceptorRegister // todo: internal
    {
        private Type _latestType;

        public static Action<Type, Type, ServiceInstanceScope> Inspector { get; set; }

        public DryIocServiceConfigurationProvider(IContainer container) => Container = container;

        public IContainer Container { get; }

        public IRegisteredService Add(Type serviceType, Type implementationType, ServiceInstanceScope lifetime)
        {
            if (serviceType is null) { throw new ArgumentNullException(nameof(serviceType)); }
            if (implementationType is null) { throw new ArgumentNullException(nameof(implementationType), $"{serviceType?.FullName ?? "no service type"} was not given an implementation type!"); }

            Inspector?.Invoke(serviceType, implementationType, lifetime);
            Container.Register(serviceType, implementationType, ConvertLifeTime(lifetime));

            //if (implementationType.GetInterfaces() is Type[] interfaces && interfaces.Length > 1)
            //{
            //    foreach (var t in interfaces)
            //    {
            //        if (t == serviceType) { continue; }

            //        try
            //        {
            //            Container.RegisterMapping(t, serviceType, factoryType: FactoryType.Service);
            //        }
            //        catch 
            //        {
            //        }// todo: bad
            //    }
            //}

            _latestType = serviceType;
            return this;
        }

        public IRegisteredService Add(Type serviceType, Func<IServiceLocator, object> implementationFactory, ServiceInstanceScope lifetime)
        {
            if(implementationFactory is null) { throw new ArgumentNullException(nameof(implementationFactory)); }

            if(implementationFactory.Method.Name.StartsWith("<Forward"))
            {

            }

            Inspector?.Invoke(serviceType, implementationFactory.GetType(), lifetime);

            object checkedDelegate(IResolverContext r)
            {                
                var obj = (object)implementationFactory(r.Resolve<IServiceLocator>());
                if (obj == null)
                {
                    

                    var lf = lifetime;
                }
                return obj
                    .ThrowIfNotInstanceOf(serviceType, Error.RegisteredDelegateResultIsNotOfServiceType, r);
            }


            var factory = new DelegateFactory(checkedDelegate, ConvertLifeTime(lifetime), null);

            Container.Register(factory, serviceType, null, null, isStaticallyChecked: false);

            //Container.RegisterDelegate(serviceType, r =>
            //{
            //    try
            //    {
            //        return implementationFactory(r.Resolve<IServiceLocator>());
            //    }
            //    catch(Exception e)
            //    {
            //        throw new Exception("brad", e);
            //    }
            //},
            //ConvertLifeTime(lifetime));

            _latestType = serviceType;
            return this;
        }

        public IRegisteredService Add(Type serviceType, object instance)
        {

            Inspector?.Invoke(serviceType, instance.GetType(), ServiceInstanceScope.Singleton);
            Container.RegisterInstance(serviceType, instance);
            _latestType = serviceType;

            return this;
        }

        public IServiceConfigurationProvider AddServiceAccessor()
        {
            // todo: internal
            EPiServer.ServiceLocation.Internal.
                ReflectiveServiceConfigurationHelper.RegisterServiceAccessorDelegates(this, _latestType);

            return this;
        }

        public void Verify()
        {
            (Container as Container).Validate();
        }

        public bool Contains(Type serviceType) => Container.IsRegistered(serviceType);

        public void Intercept<T>(Func<IServiceLocator, T, T> interceptorFactory) where T : class =>
            Container.RegisterDelegateDecorator<T>(r => (t) => interceptorFactory(r.Resolve<IServiceLocator>(), t));

        public IServiceConfigurationProvider RemoveAll(Type serviceType)
        {
            Container.Unregister(serviceType, null, FactoryType.Service, (f) => true);

            return this;
        }

        private static IReuse ConvertLifeTime(ServiceInstanceScope lifetime)
        {
            switch (lifetime)
            {
                case ServiceInstanceScope.Singleton:
                    return Reuse.Singleton;
#pragma warning disable CS0618 // Type or member is obsolete
                case ServiceInstanceScope.Unique:
                case ServiceInstanceScope.PerRequest:
#pragma warning restore CS0618 // Type or member is obsolete
                case ServiceInstanceScope.Transient:
                    return Reuse.Transient;
                case ServiceInstanceScope.HttpContext:
                case ServiceInstanceScope.Hybrid:
                    return Reuse.Scoped;
            }

            throw new NotSupportedException(lifetime.ToString() + " is not supported!");
        }
    }
}