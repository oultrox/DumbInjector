using UnityEngine;

namespace DumbInjector.Injectors
{
    /// <summary>
    /// Component that auto-injects newly instantiated GameObjects.
    /// Works with SceneContextInjector or global Injector.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(int.MinValue + 1000)]
    public class AutoInjector : MonoBehaviour
    {
        public enum InjectionMode
        {
            Single,   // inject only the first MonoBehaviour
            Object,   // inject all MonoBehaviours on this GameObject
            Recursive // inject all MonoBehaviours on this GameObject and children
        }

        public InjectionMode mode = InjectionMode.Recursive;

        private void Awake()
        {
            IInjector injector = FindSceneOrGlobalInjector();
            if (injector != null)
            {
                InjectAll(injector);
            }
            else
            {
                Debug.LogWarning($"No injector found for AutoInjector on {gameObject.name}. " +
                                 $"Make sure a SceneContextInjector or GlobalContextInjector exists.");
            }
        }

        private IInjector FindSceneOrGlobalInjector()
        {
            // Try scene-local injector first
            var sceneInjector = FindObjectOfType<SceneContextInjector>();
            if (sceneInjector != null) return sceneInjector;
            if (Builder.Instance != null) return Builder.Instance;

            return null;
        }

        private void InjectAll(IInjector injector)
        {
            switch (mode)
            {
                case InjectionMode.Single:
                    var mb = GetComponent<MonoBehaviour>();
                    if (mb != null) injector.Inject(mb);
                    break;
                case InjectionMode.Object:
                    foreach (var component in GetComponents<MonoBehaviour>())
                        injector.Inject(component);
                    break;
                case InjectionMode.Recursive:
                    foreach (var component in GetComponentsInChildren<MonoBehaviour>())
                        injector.Inject(component);
                    break;
            }
        }
    }
}
