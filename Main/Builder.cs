using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DumbInjector.Internal;
using UnityEngine;

namespace DumbInjector
{
    /// <summary>
    /// Global container.
    /// </summary>
    [DefaultExecutionOrder(int.MinValue + 999)]
    public class Builder : Singleton<Builder>, IInjector
    {
        const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly Dictionary<Type, object> _registry = new();
        readonly HashSet<Type> _injectableTypes = new();
        bool _typesCached;
        
        public void Inject(object instance)
        {
            var type = instance.GetType();
            if (!_injectableTypes.Contains(type)) return;

            InjectFields(instance, type);
            InjectMethods(instance, type);
            InjectProperties(instance, type);
        }

        public object Resolve(Type t)
        {
            _registry.TryGetValue(t, out var resolved);
            return resolved;
        }

        public void RegisterProvider(IDependencyProvider provider)
        {
            var methods = provider.GetType().GetMethods(BINDING_FLAGS)
                .Where(m => Attribute.IsDefined(m, typeof(ProvideAttribute)));

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                if (_registry.ContainsKey(returnType))
                {
                    Debug.LogWarning($"Provider for {returnType.Name} already registered. Ignoring duplicate.");
                    continue;
                }

                var instance = method.Invoke(provider, null);
                if (instance == null)
                    throw new Exception($"Provider {provider.GetType().Name} returned null for {returnType.Name}");

                _registry.Add(returnType, instance);
                Debug.Log($"Injected {returnType.Name} from {provider.GetType().Name}");
            }
        }

        void InjectFields(object instance, Type type)
        {
            foreach (var field in type.GetFields(BINDING_FLAGS).Where(f => Attribute.IsDefined(f, typeof(InjectAttribute))))
            {
                var fieldType = field.FieldType;
                var resolved = Resolve(fieldType);
                if (resolved == null) throw new Exception($"Failed to inject {fieldType.Name} for {type.Name}");
                field.SetValue(instance, resolved);
            }
        }

        void InjectMethods(object instance, Type type)
        {
            foreach (var method in type.GetMethods(BINDING_FLAGS).Where(m => Attribute.IsDefined(m, typeof(InjectAttribute))))
            {
                var parameters = method.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray();
                if (parameters.Any(p => p == null))
                    throw new Exception($"Failed to inject method {type.Name}.{method.Name}");
                method.Invoke(instance, parameters);
            }
        }

        void InjectProperties(object instance, Type type)
        {
            foreach (var prop in type.GetProperties(BINDING_FLAGS).Where(p => Attribute.IsDefined(p, typeof(InjectAttribute))))
            {
                if (!prop.CanWrite) throw new Exception($"Property {type.Name}.{prop.Name} is not writable");
                var resolved = Resolve(prop.PropertyType);
                if (resolved == null) throw new Exception($"Failed to inject property {type.Name}.{prop.Name}");
                prop.SetValue(instance, resolved);
            }
        }
    }
}
