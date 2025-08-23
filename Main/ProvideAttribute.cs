using System;

namespace DumbInjector
{
    /// <summary>
    /// Marks a method as a provider of a dependency.
    /// Methods with this attribute will be invoked by the injector
    /// to retrieve instances that can be injected elsewhere.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ProvideAttribute : Attribute
    {
    }
}