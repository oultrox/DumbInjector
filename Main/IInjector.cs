using System;

namespace DumbInjector
{
    /// <summary>
    /// Interface for all injectors.
    /// </summary>
    public interface IInjector
    {
        void Inject(object mb);
        object Resolve(Type t);
    }
}