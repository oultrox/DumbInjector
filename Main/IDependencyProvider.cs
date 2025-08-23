namespace DumbInjector
{
    /// <summary>
    /// Marks a class as a provider of dependencies for the injector system.
    /// 
    /// Classes implementing this interface can expose methods annotated with 
    /// <see cref="ProvideAttribute"/> to supply instances that will be automatically 
    /// registered and injected into other objects.
    /// </summary>
    public interface IDependencyProvider {}
}