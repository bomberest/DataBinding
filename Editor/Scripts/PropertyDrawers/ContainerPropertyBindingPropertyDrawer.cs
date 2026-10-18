using UnityEditor;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomPropertyDrawer(typeof(ContainerPropertyBinding))]
  public class ContainerPropertyBindingPropertyDrawer : PropertyDrawer
  {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
      BindingInspectorFields.Reactive(property, Build);

    private static VisualElement Build(SerializedProperty property)
    {
      var binding = (ContainerPropertyBinding)property.boxedValue;
      var modelType = BindingInspectorFields.ModelType(property);
      var (state, _) = DataBindingCommonData.DetermineContainerPropertyBindingState(binding, modelType);
      var source = string.IsNullOrEmpty(binding.SourcePath) ? "Items" : binding.SourcePath;
      var target = binding.TargetContainer != null ? binding.TargetContainer.name : "Container";
      var foldout = BindingInspectorFields.Foldout(property, $"{source} → {target}");
      var sourceType = DataBindingCommonData.GetPathProperty(modelType, binding.SourcePath)?.PropertyType;
      var viewSource = binding.ViewSelector != null ? $"Selector: {binding.ViewSelector.name}"
        : sourceType != null && DataBindingCommonData.HasViewProviderElements(sourceType) ? "View from model"
        : binding.ElementTemplate != null ? $"Default: {binding.ElementTemplate.name}" : "Choose a view prefab";
      foldout.Add(BindingInspectorFields.Secondary(
        $"{sourceType?.GetFriendlyName() ?? "Collection binding"} · {viewSource}"));
      foldout.Add(BindingInspectorFields.Source(property.FindPropertyRelative("sourcePath"), true));
      foldout.Add(BindingInspectorFields.Property(property, "targetContainer", "Container"));
      var section = new Label("View Selection");
      section.AddToClassList("db-field-section");
      foldout.Add(section);
      foldout.Add(BindingInspectorFields.Property(property, "viewSelector", "Selector"));
      foldout.Add(BindingInspectorFields.Property(property, "elementTemplate", "Default Prefab"));
      var priority = BindingInspectorFields.Secondary("Selector → Model → Default Prefab");
      priority.tooltip = "A null result falls through to the next option. The selector has priority over IViewProvider.";
      foldout.Add(priority);
      if (state != ContainerPropertyBindingState.Complete)
      {
        var message = modelType == null ? "Select a model first."
          : state == ContainerPropertyBindingState.ElementTemplateMissing
            ? "Assign a selector or default prefab, or use models implementing IViewProvider."
          : state == ContainerPropertyBindingState.TargetUnbound ? "Assign the container for item views."
          : "Choose a collection of models as the items source.";
        foldout.Add(new HelpBox(message, HelpBoxMessageType.Info));
      }
      foldout.tooltip = state == ContainerPropertyBindingState.Complete
        ? "Binding is configured; dynamic prefabs are resolved at runtime" : "Binding needs configuration";
      return foldout;
    }
  }
}
