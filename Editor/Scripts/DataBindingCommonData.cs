using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JH.DataBinding.Editor
{
    internal static class DataBindingCommonData
    {
        public static string DefaultDataSourceTemplateNamePlaceHolder => "{{name}}";
        public static string DefaultDataSourceTemplate =>
            File.ReadAllText(
                "Packages/de.jochenheckl.unity.databinding/Editor/DefaultDataSourceTemplate.txt"
            );

        public static string dataSourceTypeInspectorFilter { get; set; } = string.Empty;

        internal static readonly IDataBindingEditorDisplayText EditorDisplayText =
            new DataBindingEditorDisplayText();

        private static Type[] cachedDataSourceTypes;

        internal static Type[] GetValidDataSourceTypes()
        {
            return cachedDataSourceTypes ??= TypeCache
                .GetTypesDerivedFrom<INotifyDataSourceChanged>()
                .Where(x => x.IsVisible && !x.IsAbstract && !x.ContainsGenericParameters && !x.IsInterface)
                .OrderBy(x => x.GetFriendlyName(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.FullName, StringComparer.Ordinal)
                .ThenBy(x => x.Assembly.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        internal static Type GuessDataSourceTypeName(string viewName)
        {
            return GetValidDataSourceTypes()
                .Select(x => new { x, distance = viewName.DamerauLevenshteinDistance(x.Name) })
                .OrderBy(x => x.distance)
                .Select(x => x.x)
                .FirstOrDefault();
        }

        internal static string FindDataSourceSourceFile(Type type)
        {
            if (type == null) return null;
            while (type.DeclaringType != null) type = type.DeclaringType;
            var candidates = AssetDatabase.FindAssets($"{type.Name} t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => new { path, script = AssetDatabase.LoadAssetAtPath<MonoScript>(path) })
                .Where(x => x.script != null);
            return candidates.FirstOrDefault(x => x.script.GetClass() == type)?.path;
        }

        internal static (
            ComponentPropertyBindingState bindingState,
            PropertyInfo[] bindableProperties
        ) DetermineComponentPropertyBindingState(SerializedProperty serializedProperty)
        {
            var binding = serializedProperty.boxedValue as ComponentPropertyBinding;

            var view = serializedProperty.serializedObject.targetObject as View;

            if (view == null)
            {
                throw new InvalidOperationException(
                    "The provided ComponentPropertyBinding must be part of a view to dermine the binding state."
                );
            }

            return DetermineComponentPropertyBindingState(binding, view.dataSourceType?.Type);
        }

        internal static (
            ComponentPropertyBindingState bindingState,
            PropertyInfo[] bindableProperties
        ) DetermineComponentPropertyBindingState(
            ComponentPropertyBinding binding,
            Type dataSourceType
        )
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (dataSourceType == null)
            {
                return (
                    ComponentPropertyBindingState.MissingDataSourceAssignment,
                    Array.Empty<PropertyInfo>()
                );
            }

            var bindableDataSourceProperties = GetBindableDataSourceProperties(dataSourceType);

            if (bindableDataSourceProperties.Length == 0)
            {
                return (
                    ComponentPropertyBindingState.NoBindableProperties,
                    bindableDataSourceProperties
                );
            }

            var sourceProperty = GetPathProperty(dataSourceType, binding.SourcePath);

            if (sourceProperty == null)
            {
                return (ComponentPropertyBindingState.SourceUnbound, bindableDataSourceProperties);
            }

            var targetProperty =
                binding.TargetComponent != null
                    ? binding
                        .TargetComponent.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(x => x.Name == binding.TargetPath
                            && x.GetSetMethod() != null && x.GetIndexParameters().Length == 0)
                    : null;

            if (targetProperty == null)
            {
                return (ComponentPropertyBindingState.TargetUnbound, bindableDataSourceProperties);
            }

            if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
            {
                return (ComponentPropertyBindingState.Unassignable, bindableDataSourceProperties);
            }

            return (ComponentPropertyBindingState.Complete, bindableDataSourceProperties);
        }

        internal static (
            ContainerPropertyBindingState bindingState,
            PropertyInfo[] bindableProperties
        ) DetermineContainerPropertyBindingState(SerializedProperty serializedProperty)
        {
            var binding = serializedProperty.boxedValue as ContainerPropertyBinding;

            var view = serializedProperty.serializedObject.targetObject as View;

            if (view == null)
            {
                throw new InvalidOperationException(
                    "The provided ContainerPropertyBinding must be part of a view to dermine the binding state."
                );
            }

            return DetermineContainerPropertyBindingState(binding, view.dataSourceType?.Type);
        }

        internal static (
            ContainerPropertyBindingState bindingState,
            PropertyInfo[] bindableDataSourceProperties
        ) DetermineContainerPropertyBindingState(
            ContainerPropertyBinding binding,
            Type dataSourceType
        )
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (dataSourceType == null)
            {
                return (
                    ContainerPropertyBindingState.MissingDataSourceAssignment,
                    Array.Empty<PropertyInfo>()
                );
            }

            var bindableDataSourceProperties = GetBindableDataSourceEnumerableProperties(
                dataSourceType
            );

            if (bindableDataSourceProperties.Length == 0)
            {
                return (
                    ContainerPropertyBindingState.NoBindableProperties,
                    bindableDataSourceProperties
                );
            }

            var sourceProperty = GetPathProperty(dataSourceType, binding.SourcePath);

            if (sourceProperty == null
                || !typeof(IEnumerable<INotifyDataSourceChanged>).IsAssignableFrom(sourceProperty.PropertyType))
            {
                return (ContainerPropertyBindingState.SourceUnbound, bindableDataSourceProperties);
            }

            if (binding.TargetContainer == null)
            {
                return (ContainerPropertyBindingState.TargetUnbound, bindableDataSourceProperties);
            }

            if (binding.ElementTemplate == null
                && binding.ViewSelector == null
                && !HasViewProviderElements(sourceProperty.PropertyType))
            {
                return (
                    ContainerPropertyBindingState.ElementTemplateMissing,
                    bindableDataSourceProperties
                );
            }

            if (binding.ElementTemplate != null
                && binding.ElementTemplate.GetComponent<View>() == null)
            {
                return (
                    ContainerPropertyBindingState.ElementTemplateIsNotAssignable,
                    bindableDataSourceProperties
                );
            }

            return (ContainerPropertyBindingState.Complete, bindableDataSourceProperties);
        }

        internal static bool HasViewProviderElements(Type collectionType)
        {
            return collectionType.GetInterfaces().Concat(new[] { collectionType }).Any(type =>
                type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                && typeof(IViewProvider).IsAssignableFrom(type.GetGenericArguments()[0])
            );
        }

        internal static PropertyInfo[] GetBindableDataSourceProperties(Type dataSourceType)
        {
            return dataSourceType?.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetGetMethod() != null && x.GetIndexParameters().Length == 0)
                .ToArray() ?? Array.Empty<PropertyInfo>();
        }

        internal static PropertyInfo[] GetBindableDataSourceEnumerableProperties(
            Type dataSourceType
        )
        {
            return GetBindableDataSourceProperties(dataSourceType)
                .Where(x => typeof(IEnumerable<INotifyDataSourceChanged>).IsAssignableFrom(x.PropertyType))
                .ToArray();
        }

        internal static PropertyInfo[] GetBindableComponentProperties(
            Component component,
            Type sourcePropertyType
        )
        {
            if (component == null || sourcePropertyType == null) return Array.Empty<PropertyInfo>();
            var assignableProperties = component
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetSetMethod() != null && x.GetIndexParameters().Length == 0
                    && x.PropertyType.IsAssignableFrom(sourcePropertyType))
                .ToArray();

            return assignableProperties;
        }

        internal static PropertyInfo GetPathProperty(Type type, string path)
        {
            if (type == null || string.IsNullOrEmpty(path)) return null;
            PropertyInfo property = null;
            foreach (var segment in path.Split('.'))
            {
                property = type.GetProperty(segment, BindingFlags.Instance | BindingFlags.Public);
                if (property?.GetGetMethod() == null || property.GetIndexParameters().Length != 0)
                    return null;
                type = property.PropertyType;
            }
            return property;
        }

        internal static string GetComponentDisplayName(Component component)
        {
            if (component != null)
            {
                return $"{component.GetType().Name} ({component.name})";
            }
            else
            {
                return null;
            }
        }
    }
}
