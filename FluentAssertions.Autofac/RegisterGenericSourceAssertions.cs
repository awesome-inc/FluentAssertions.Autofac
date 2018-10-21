using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using FluentAssertions.Primitives;

namespace FluentAssertions.Autofac
{
    /// <inheritdoc />
    /// <summary>
    ///     Contains a number of methods to assert that an <see cref="T:Autofac.IContainer" /> is in the expected state.
    /// </summary>
#if !DEBUG
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public class RegisterGenericSourceAssertions : ReferenceTypeAssertions<IContainer, RegisterGenericSourceAssertions>
    {
        private readonly Type _genericComponentTypeDefinition;

        /// <inheritdoc />
        /// <summary>
        ///     Returns the type of the subject the assertion applies on.
        /// </summary>
#if !NETSTANDARD_1X
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        protected override string Identifier => nameof(IContainer);

        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterGenericSourceAssertions" /> class.
        /// </summary>
        /// <param name="subject">The container</param>
        /// <param name="genericComponentTypeDefinition">The type that should be registered on the container</param>
        public RegisterGenericSourceAssertions(IContainer subject, Type genericComponentTypeDefinition)
        {
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            AssertGenericType(genericComponentTypeDefinition);
            _genericComponentTypeDefinition = genericComponentTypeDefinition;
        }

        /// <summary>
        ///   Asserts that the specified service type can be resolved from the current <see cref="IContainer"/>.
        /// </summary>
        /// <param name="genericServiceTypeDefinition">The type to resolve</param>
        public RegistrationAssertions As(Type genericServiceTypeDefinition)
        {
            AssertGenericType(genericServiceTypeDefinition);
            var componentServicePairText = $"Component={_genericComponentTypeDefinition.FullName} Service={genericServiceTypeDefinition.FullName}";

            _genericComponentTypeDefinition.GetGenericArguments().Length.Should()
                .Be(genericServiceTypeDefinition.GetGenericArguments().Length,
                    $"the generic arguments count of both generic component and generic service must be equal. {componentServicePairText}.");

            var argumentTypes = Enumerable.Repeat(typeof(object),
                _genericComponentTypeDefinition.GetGenericArguments().Length).ToArray();
            var componentType = _genericComponentTypeDefinition.MakeGenericType(argumentTypes);
            var serviceType = genericServiceTypeDefinition.MakeGenericType(argumentTypes);

            componentType.Should()
                .Implement(serviceType,
                    $"component must implement specified service. {componentServicePairText}.");

            var registration = GetRegistrationFromSources(serviceType);
            registration.Should()
                .NotBeNull(
                    $"it must be a registration source providing registrations for service {genericServiceTypeDefinition.FullName}");

            registration.Activator.LimitType.Should()
                .Be(componentType, $"the generic component type definition in the registration must be {_genericComponentTypeDefinition.FullName}.");

            return new RegistrationAssertions(Subject, registration);
        }

        private IComponentRegistration GetRegistrationFromSources(Type serviceType)
        {
            var typedService = new TypedService(serviceType);

            foreach (var registrationSource in Subject.ComponentRegistry.Sources)
            {
                var registration = registrationSource
                    .RegistrationsFor(typedService, Subject.ComponentRegistry.RegistrationsFor)
                    .FirstOrDefault();

                if (registration != null)
                {
                    return registration;
                }
            }

            return null;
        }

        private static void AssertGenericType(Type genericTypeDefinition)
        {
            if (genericTypeDefinition == null) throw new ArgumentNullException(nameof(genericTypeDefinition));
#if NETSTANDARD_1X
            throw new NotSupportedException("Not supported in .NET Standard 1.x");
#else
            if (!genericTypeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException("Type must be a generic type definition.",
                    nameof(genericTypeDefinition));
#endif
        }
    }
}