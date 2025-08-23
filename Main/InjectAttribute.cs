using System;

namespace DumbInjector
{
    /// <summary>
    /// Marks a field or method for dependency injection.
    /// Fields or methods annotated with this attribute will have their dependencies 
    /// automatically resolved and assigned by the injector system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class InjectAttribute : Attribute
    {
    }
}