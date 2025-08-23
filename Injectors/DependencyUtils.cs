using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DumbInjector.Injectors
{
    /// <summary>
    /// Utility class providing methods for dependency injection.
    /// Handles injecting [Inject] members and registering provider outputs.
    /// </summary>
    public static class DependencyUtils
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Injects dependencies into all fields, properties, and methods
        /// marked with the [Inject] attribute.
        /// </summary>
        /// <param name="instance">The object to inject into.</param>
        /// <param name="resolver">Function to resolve dependencies by type.</param>
        public static void Inject(object instance, Func<Type, object> resolver)
        {
            var type = instance.GetType();

            InjectFields(instance, type, resolver);
            InjectProperties(instance, type, resolver);
            InjectMethods(instance, type, resolver);
        }

        /// <summary>
        /// Registers all outputs from a dependency provider into a registry,
        /// injecting dependencies into the provided instances first so they don't get handed half-baked.
        /// </summary>
        /// <param name="provider">The provider object to register outputs from.</param>
        /// <param name="registry">Dictionary to register the instances into.</param>
        /// <param name="injectAction">Action used to inject dependencies into the provided instances.</param>
        public static void RegisterProvider(IDependencyProvider provider, Dictionary<Type, object> registry, Action<object> injectAction)
        {
            var methods = provider.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods.Where(m => Attribute.IsDefined(m, typeof(ProvideAttribute))))
            {
                var returnType = method.ReturnType;
                var providedInstance = method.Invoke(provider, null);
                if (providedInstance != null)
                {
                    // Inject first
                    injectAction(providedInstance);

                    // Register by return type (silent)
                    registry.TryAdd(returnType, providedInstance);

                    // Register interfaces (silent)
                    foreach (var iface in returnType.GetInterfaces())
                    {
                        registry.TryAdd(iface, providedInstance);
                    }
                }
            }
        }

        /// <summary>
        /// Returns all types in the current domain that are injectable,
        /// i.e., MonoBehaviours with [Inject] members or implementing IDependencyProvider.
        /// </summary>
        /// <returns>A set of injectable types.</returns>
        public static HashSet<Type> GetInjectableTypes()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t));

            var injectableTypes = new HashSet<Type>();

            foreach (var t in types)
            {
                bool injectable = t.GetFields(flags).Any(f => Attribute.IsDefined(f, typeof(InjectAttribute))) ||
                                  t.GetProperties(flags).Any(p => Attribute.IsDefined(p, typeof(InjectAttribute))) ||
                                  t.GetMethods(flags).Any(m => Attribute.IsDefined(m, typeof(InjectAttribute))) ||
                                  typeof(IDependencyProvider).IsAssignableFrom(t);
                if (injectable) injectableTypes.Add(t);
            }

            return injectableTypes;
        }
        
        static void InjectFields(object instance, Type type, Func<Type, object> resolver)
        {
            foreach (var field in type.GetFields(FLAGS).Where(f => Attribute.IsDefined(f, typeof(InjectAttribute))))
            {
                var resolved = resolver(field.FieldType);
                if (resolved != null)
                    field.SetValue(instance, resolved);
            }
        }

        static void InjectProperties(object instance, Type type, Func<Type, object> resolver)
        {
            foreach (var prop in type.GetProperties(FLAGS).Where(p => Attribute.IsDefined(p, typeof(InjectAttribute))))
            {
                if (!prop.CanWrite) continue;
                var resolved = resolver(prop.PropertyType);
                if (resolved != null)
                    prop.SetValue(instance, resolved);
            }
        }

        static void InjectMethods(object instance, Type type, Func<Type, object> resolver)
        {
            foreach (var method in type.GetMethods(FLAGS).Where(m => Attribute.IsDefined(m, typeof(InjectAttribute))))
            {
                var parameters = method.GetParameters().Select(p => resolver(p.ParameterType)).ToArray();
                if (parameters.All(p => p != null))
                    method.Invoke(instance, parameters);
            }
        }
    }
}
