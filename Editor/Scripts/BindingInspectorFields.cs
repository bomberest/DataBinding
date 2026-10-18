using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  internal static class BindingInspectorFields
  {
    internal static Type ModelType(SerializedProperty property) =>
      (property.serializedObject.targetObject as View)?.dataSourceType?.Type;

    internal static VisualElement Reactive(SerializedProperty property,
      Func<SerializedProperty, VisualElement> build)
    {
      var root = new VisualElement();
      if (DataBindingEditorStyle.StyleSheet != null)
        root.styleSheets.Add(DataBindingEditorStyle.StyleSheet);
      var serialized = property.serializedObject;
      var path = property.propertyPath;
      string previous = null;
      void Refresh()
      {
        if (serialized.targetObject == null) return;
        var current = serialized.FindProperty(path);
        if (current == null) return;
        var model = (serialized.targetObject as View)?.dataSourceType?.assemblyQualifiedName;
        var snapshot = model + JsonUtility.ToJson(current.boxedValue);
        if (snapshot == previous) return;
        previous = snapshot;
        root.Clear();
        root.Add(build(current));
      }
      root.TrackSerializedObjectValue(serialized, _ => Refresh());
      Refresh();
      return root;
    }

    internal static VisualElement Source(SerializedProperty property, bool collection)
    {
      var properties = collection
        ? DataBindingCommonData.GetBindableDataSourceEnumerableProperties(ModelType(property))
        : DataBindingCommonData.GetBindableDataSourceProperties(ModelType(property));
      return StringChoice(property, collection ? "Items Source" : "Source",
        properties.Select(x => x.Name), ModelType(property) != null);
    }

    internal static DropdownField StringChoice(SerializedProperty property, string label,
      IEnumerable<string> values, bool enabled = true)
    {
      var choices = new List<string> { "— Select —" };
      choices.AddRange(values.Distinct());
      if (!string.IsNullOrEmpty(property.stringValue) && !choices.Contains(property.stringValue))
        choices.Add(property.stringValue);
      var field = new DropdownField(label, choices,
        string.IsNullOrEmpty(property.stringValue) ? 0 : choices.IndexOf(property.stringValue));
      field.AddToClassList("unity-base-field__aligned");
      field.SetEnabled(enabled);
      field.RegisterValueChangedCallback(evt =>
      {
        property.stringValue = evt.newValue == choices[0] ? "" : evt.newValue;
        property.serializedObject.ApplyModifiedProperties();
      });
      return field;
    }

    internal static VisualElement TargetObject(SerializedProperty binding)
    {
      var property = binding.FindPropertyRelative("targetGameObject");
      var field = new ObjectField("Target Object")
      {
        objectType = typeof(GameObject),
        allowSceneObjects = true,
        value = property.objectReferenceValue,
      };
      field.AddToClassList("unity-base-field__aligned");
      field.RegisterValueChangedCallback(evt =>
      {
        property.objectReferenceValue = evt.newValue;
        binding.FindPropertyRelative("targetComponent").objectReferenceValue = null;
        binding.FindPropertyRelative("targetPath").stringValue = "";
        property.serializedObject.ApplyModifiedProperties();
      });
      return field;
    }

    internal static VisualElement TargetComponent(SerializedProperty binding)
    {
      var property = binding.FindPropertyRelative("targetComponent");
      var sourceType = DataBindingCommonData.GetPathProperty(ModelType(binding),
        binding.FindPropertyRelative("sourcePath").stringValue)?.PropertyType;
      var target = binding.FindPropertyRelative("targetGameObject").objectReferenceValue as GameObject;
      var candidates = target != null
        ? target.GetComponentsInChildren<Component>(true)
          .Where(x => x != null && DataBindingCommonData
            .GetBindableComponentProperties(x, sourceType).Length > 0).ToList()
        : new List<Component>();
      var current = property.objectReferenceValue as Component;
      if (current != null && !candidates.Contains(current)) candidates.Add(current);
      var choices = new List<string> { "— Select —" };
      choices.AddRange(candidates.Select(component => ComponentLabel(component, target?.transform)));
      var field = new DropdownField("Component", choices, candidates.IndexOf(current) + 1);
      field.AddToClassList("unity-base-field__aligned");
      field.SetEnabled(target != null && sourceType != null);
      field.RegisterValueChangedCallback(evt =>
      {
        var index = choices.IndexOf(evt.newValue) - 1;
        property.objectReferenceValue = index >= 0 ? candidates[index] : null;
        binding.FindPropertyRelative("targetPath").stringValue = "";
        property.serializedObject.ApplyModifiedProperties();
      });
      return field;
    }

    internal static string ComponentLabel(Component component, Transform root)
    {
      var names = new List<string>();
      var transform = component.transform;
      while (transform != null)
      {
        var peers = transform.parent != null
          ? transform.parent.Cast<Transform>().Where(x => x.name == transform.name).ToArray()
          : Array.Empty<Transform>();
        var name = transform.name;
        if (peers.Length > 1) name += " [" + (Array.IndexOf(peers, transform) + 1) + "]";
        names.Insert(0, name);
        if (transform == root) break;
        transform = transform.parent;
      }
      var sameType = component.GetComponents(component.GetType());
      var index = Array.IndexOf(sameType, component) + 1;
      return $"{component.GetType().Name}{(sameType.Length > 1 ? " #" + index : "")} — {string.Join("/", names)}";
    }

    internal static VisualElement TargetPath(SerializedProperty binding)
    {
      var component = binding.FindPropertyRelative("targetComponent").objectReferenceValue as Component;
      var sourceType = DataBindingCommonData.GetPathProperty(ModelType(binding),
        binding.FindPropertyRelative("sourcePath").stringValue)?.PropertyType;
      return StringChoice(binding.FindPropertyRelative("targetPath"), "Property",
        DataBindingCommonData.GetBindableComponentProperties(component, sourceType)
          .Select(x => x.Name), component != null && sourceType != null);
    }

    internal static PropertyField Property(SerializedProperty parent, string name, string label)
    {
      var property = parent.FindPropertyRelative(name);
      var field = new PropertyField(property, label);
      field.BindProperty(property);
      return field;
    }

    internal static Foldout Foldout(SerializedProperty property, string title)
    {
      var foldout = new Foldout { text = title, value = property.isExpanded };
      foldout.AddToClassList("db-binding-foldout");
      foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);
      return foldout;
    }

    internal static Label Secondary(string text)
    {
      var label = new Label(text);
      label.AddToClassList("db-secondary");
      label.tooltip = text;
      return label;
    }
  }
}
