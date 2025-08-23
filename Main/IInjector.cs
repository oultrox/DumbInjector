using System;

namespace DumbInjector
{
    /// <summary>
    /// Interface defining a dependency injector.
    /// Implementing classes can inject dependencies into objects
    /// and resolve instances by type.
    /// </summary>
    public interface IInjector
    {
        /// <summary>
        /// Injects dependencies into the specified object.
        /// </summary>
        /// <param name="mb">The object into which dependencies will be injected.</param>
        void Inject(object mb);

        /// <summary>
        /// Resolves and returns an instance of the specified type.
        /// </summary>
        /// <param name="t">The type of the instance to resolve.</param>
        /// <returns>The resolved instance, or null if not found.</returns>
        object Resolve(Type t);
    }
}