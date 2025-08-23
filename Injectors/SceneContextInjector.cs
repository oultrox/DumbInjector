using System;
using System.Collections.Generic;
using UnityEngine;

namespace DumbInjector.Injectors
    {
        /// <summary>
        /// Scene injector: handles scene-local objects for faster lookups.
        /// </summary>
        [DefaultExecutionOrder(int.MinValue + 1000)]
        public class SceneContextInjector : MonoBehaviour, IInjector
        {
            readonly Dictionary<Type, object> _sceneRegistry = new();
            HashSet<Type> _injectableTypes = new();
            bool _areTypesCached;

            void Awake()
            {
                _injectableTypes = DependencyUtils.GetInjectableTypes();
                InjectSceneObjects();
            }
            
            void InjectSceneObjects()
            {
                var roots = gameObject.scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    var all = root.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var mb in all)
                    {
                        if (mb == null) continue;

                        // Register provider outputs
                        if (mb is IDependencyProvider provider)
                            DependencyUtils.RegisterProvider(provider, _sceneRegistry, Inject);

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
            
            public void Inject(object instance)
            {
                DependencyUtils.Inject(instance, Resolve);
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
            
            bool IsInjectable(MonoBehaviour obj)
            {
                return  _injectableTypes.Contains(obj.GetType());
            }
        }
    }
