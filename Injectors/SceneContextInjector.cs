using System;
using System.Collections.Generic;
using System.Linq;
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

                // Collect all MonoBehaviours in the scene
                var allMBs = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(mb => mb != null)
                    .ToArray();

                // First pass: register all IDependencyProvider outputs
                foreach (var mb in allMBs)
                {
                    if (mb is IDependencyProvider provider)
                    {
                        DependencyUtils.RegisterProvider(provider, _sceneRegistry, Inject);
                    }
                }

                // Second pass: register all scene objects themselves and their interfaces
                foreach (var mb in allMBs)
                {
                    var type = mb.GetType();
                    _sceneRegistry.TryAdd(type, mb);

                    foreach (var iface in type.GetInterfaces())
                        _sceneRegistry.TryAdd(iface, mb);
                }

                // Third pass: inject all MonoBehaviours that have [Inject] fields/properties/methods
                foreach (var mb in allMBs)
                {
                    if (IsInjectable(mb))
                    {
                        Inject(mb);
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
