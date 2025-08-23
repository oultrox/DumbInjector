using System;
using System.Collections.Generic;
using DumbInjector.Internal;
using UnityEngine;

namespace DumbInjector.Injectors
{
    /// <summary>
    /// Global dependency container used for a global context.
    /// </summary>
    [DefaultExecutionOrder(int.MinValue + 999)]
    public class Builder : Singleton<Builder>, IInjector
    {
        readonly Dictionary<Type, object> _registry = new();
        HashSet<Type> _injectableTypes = new();
        bool _typesCached;
        
        public void Inject(object instance)
        {
            CheckCacheTypes();
            var type = instance.GetType();
            bool isNotInjectable = !_injectableTypes.Contains(type);
            if (isNotInjectable) return;
            
            DependencyUtils.Inject(instance, Resolve);
        }
        
        public object Resolve(Type t)
        {
            _registry.TryGetValue(t, out var resolved);
            return resolved;
        }

        public void RegisterProvider(IDependencyProvider provider)
        {
            DependencyUtils.RegisterProvider(provider, _registry, Inject);
        }
        
        void CheckCacheTypes()
        {
            if (_typesCached) return;
            _injectableTypes = DependencyUtils.GetInjectableTypes();
            _typesCached = true;
        }
    }
}
