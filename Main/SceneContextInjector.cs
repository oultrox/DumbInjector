using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DumbInjector
{
    /// <summary>
    /// Scene injector: handles scene-local objects for faster lookups.
    /// </summary>
    [DefaultExecutionOrder(int.MinValue + 1000)]
    public class SceneContextInjector : MonoBehaviour, IInjector
    {
        readonly HashSet<Type> _injectableTypes = new();
        readonly Dictionary<Type, object> _sceneRegistry = new();
        bool _areTypesCached;

        private void Awake()
        {
            CacheInjectableTypes();
            InjectSceneObjects();
        }

        void CacheInjectableTypes()
        {
            if (_areTypesCached) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t));

            foreach (var t in types)
            {
                bool injectable = t.GetFields(flags).Any(f => Attribute.IsDefined(f, typeof(InjectAttribute))) ||
                                  t.GetProperties(flags).Any(p => Attribute.IsDefined(p, typeof(InjectAttribute))) ||
                                  t.GetMethods(flags).Any(m => Attribute.IsDefined(m, typeof(InjectAttribute))) ||
                                  typeof(IDependencyProvider).IsAssignableFrom(t);
                if (injectable) _injectableTypes.Add(t);
            }

            _areTypesCached = true;
        }
        
        void InjectSceneObjects()
        {
            var roots = gameObject.scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var all = root.GetComponentsInChildren<MonoBehaviour>();
                foreach (var mb in all)
                {
                    if (mb == null) continue;

                    // Register provider outputs
                    if (mb is IDependencyProvider provider)
                        RegisterProvider(provider);

                    // Register the object itself
                    var type = mb.GetType();
                    _sceneRegistry.TryAdd(type, mb);

                    // Register all interfaces it implements
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        _sceneRegistry.TryAdd(interfaceType, mb);
                    }
                    
                    if (IsInjectable(mb))
                    {
                        Inject(mb);
                    }
                    
                }
            }
        }
        
        void RegisterProvider(IDependencyProvider provider)
        {
            var methods = provider.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                var returnType = method.ReturnType;
                var providedInstance = method.Invoke(provider, null);
                if (providedInstance != null)
                {
                    _sceneRegistry.TryAdd(returnType, providedInstance);
                }
            }
        }

        public void Inject(object instance)
        {
            var type = instance.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            InjectFields(instance, type, flags);
            InjectProperties(instance, type, flags);
            InjectMethods(instance, type, flags);
        }
        
        public object Resolve(Type t)
        {
            // Check scene-local container first
            _sceneRegistry.TryGetValue(t, out var instance);

            // Fallback to global injector if one exists
            if (instance != null) return instance;
            
            var globalInjector = Builder.Exists ? Builder.Instance : null;
            if (globalInjector != null)
            {
                instance = globalInjector.Resolve(t);
            }
            return instance;
        }
        
        void InjectMethods(object instance, Type type, BindingFlags flags)
        {
            // Inject methods
            foreach (var method in type.GetMethods(flags).Where(m => Attribute.IsDefined(m, typeof(InjectAttribute))))
            {
                var parameters = method.GetParameters()
                    .Select(p => Resolve(p.ParameterType))
                    .ToArray();
                method.Invoke(instance, parameters);
            }
        }

        void InjectProperties(object instance, Type type, BindingFlags flags)
        {
            // Inject properties
            foreach (var prop in type.GetProperties(flags).Where(p => Attribute.IsDefined(p, typeof(InjectAttribute))))
            {
                if (!prop.CanWrite) continue;
                var resolved = Resolve(prop.PropertyType);
                if (resolved != null) // works for regular C# objects
                {
                    prop.SetValue(instance, resolved);
                }
            }
        }

        void InjectFields(object instance, Type type, BindingFlags flags)
        {
            // Inject fields
            foreach (var field in type.GetFields(flags).Where(f => Attribute.IsDefined(f, typeof(InjectAttribute))))
            {
                var resolved = Resolve(field.FieldType);
                if (resolved != null)
                {
                    field.SetValue(instance, resolved);
                }
            }
        }
        
        bool IsInjectable(MonoBehaviour obj)
        {
            return  _injectableTypes.Contains(obj.GetType());
        }
    }
}
