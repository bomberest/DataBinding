using UnityEditor;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomPropertyDrawer(typeof(ComponentPropertyBinding))]
  public class ComponentPropertyBindingPropertyDrawer : PropertyDrawer
  {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
      BindingInspectorFields.Reactive(property, Build);

    private static VisualElement Build(SerializedProperty property)
    {
      var binding = (ComponentPropertyBinding)property.boxedValue;
      var modelType = BindingInspectorFields.ModelType(property);
      var (state, _) = DataBindingCommonData.DetermineComponentPropertyBindingState(binding, modelType);
      var source = string.IsNullOrEmpty(binding.SourcePath) ? "Source" : binding.SourcePath;
      var target = binding.TargetComponent != null
        ? $"{binding.TargetComponent.name}.{binding.TargetPath}"
        : "Target";
      var foldout = BindingInspectorFields.Foldout(property, $"{source} → {target}");
      var type = DataBindingCommonData.GetPathProperty(modelType, binding.SourcePath)?.PropertyType;
      foldout.Add(BindingInspectorFields.Secondary(type != null
        ? $"{type.GetFriendlyName()} · {binding.TargetComponent?.GetType().Name ?? "Choose a component"}"
        : "Property binding"));
      foldout.Add(BindingInspectorFields.Source(property.FindPropertyRelative("sourcePath"), false));
      foldout.Add(BindingInspectorFields.TargetObject(property));
      foldout.Add(BindingInspectorFields.TargetComponent(property));
      foldout.Add(BindingInspectorFields.TargetPath(property));
      if (state != ComponentPropertyBindingState.Complete)
      {
        var message = modelType == null ? "Select a model first."
          : state == ComponentPropertyBindingState.Unassignable ? "Source and target property types do not match."
          : state == ComponentPropertyBindingState.SourceUnbound ? "Choose a readable source property."
          : "Choose a target component and a compatible writable property.";
        foldout.Add(new HelpBox(message, HelpBoxMessageType.Info));
      }
      foldout.tooltip = state == ComponentPropertyBindingState.Complete
        ? "Binding is configured" : "Binding needs configuration";
      return foldout;
    }
  }
}
