using UnityEngine;

namespace DumbInjector
{
    /// <summary>
    /// Interface for all injectors.
    /// </summary>
    public interface IInjector
    {
        void Inject(object mb);
        object Resolve(System.Type t);
    }
}