using System;
using System.Collections.Generic;
using UnityEngine;

namespace DumbInjector.Injectors
{
    /// <summary>
    /// Global injector: handles global services/singletons, global scoped scenes.
    /// Place in a persistent scene if cross-scene injection is needed.
    /// </summary>
    [DefaultExecutionOrder(int.MinValue + 999)]
    public class GlobalContextInjector : MonoBehaviour, IInjector
    {
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
                    {
                        Builder.Instance.RegisterProvider(provider);
                    }

                    if (IsInjectable(mb))
                    {
                        Inject(mb);
                    }
                }
            }
        }
        
        bool IsInjectable(MonoBehaviour obj)
        {
            return  _injectableTypes.Contains(obj.GetType());
        }

        public void Inject(object mb)
        {
            Builder.Instance.Inject(mb);
        }

        public object Resolve(Type t)
        {
            return Builder.Instance.Resolve(t);
        }
    }
}