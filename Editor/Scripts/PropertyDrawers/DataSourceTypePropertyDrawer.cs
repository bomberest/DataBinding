using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomPropertyDrawer(typeof(DataSourceTypeAttribute))]
  public class DataSourceTypePropertyDrawer : PropertyDrawer
  {
    internal static void SetModelType(SerializedObject serialized, string path, Type type)
    {
      if (serialized.targetObject == null) return;
      serialized.Update();
      serialized.FindProperty(path).stringValue = type?.AssemblyQualifiedName ?? "";
      serialized.ApplyModifiedProperties();
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
      var root = new VisualElement();
      root.AddToClassList("db-model");
      if (DataBindingEditorStyle.StyleSheet != null)
        root.styleSheets.Add(DataBindingEditorStyle.StyleSheet);
      var row = new VisualElement();
      row.AddToClassList("db-model-row");
      var label = new Label("Model");
      label.AddToClassList("db-field-label");
      row.Add(label);
      var picker = new Button { name = "chooseModel" };
      picker.AddToClassList("db-model-button");
      row.Add(picker);
      var open = new Button { text = "Open", tooltip = "Open the selected model's source file" };
      row.Add(open);
      row.Add(new Button(() => DataBindingEditorOperations.CreateNewDataSource(
        property.serializedObject.targetObject.name))
        { text = "New", tooltip = "Create a model source file" });
      root.Add(row);
      var context = new Label();
      context.AddToClassList("db-model-context");
      root.Add(context);
      var serialized = property.serializedObject;
      var path = property.propertyPath + ".assemblyQualifiedName";
      string sourceFile = null;
      void Refresh()
      {
        if (serialized.targetObject == null) return;
        var name = serialized.FindProperty(path)?.stringValue;
        var type = string.IsNullOrEmpty(name) ? null : Type.GetType(name);
        picker.text = type != null ? type.GetFriendlyName() + " ▾" : "Choose model… ▾";
        picker.tooltip = type?.AssemblyQualifiedName ?? "Search class, namespace or assembly";
        context.text = type != null ? DataSourceTypePicker.Context(type)
          : string.IsNullOrEmpty(name) ? "Select a model to configure its bindings."
          : "Model type is missing. Choose its replacement.";
        sourceFile = DataBindingCommonData.FindDataSourceSourceFile(type);
        open.SetEnabled(sourceFile != null);
      }
      picker.clicked += () =>
      {
        var name = serialized.FindProperty(path).stringValue;
        DataSourceTypePicker.Show(picker.worldBound,
          string.IsNullOrEmpty(name) ? null : Type.GetType(name), type =>
          {
            SetModelType(serialized, path, type);
            Refresh();
          });
      };
      open.clicked += () => DataBindingEditorOperations.OpenDataSourceEditor(sourceFile);
      root.TrackSerializedObjectValue(serialized, _ => Refresh());
      Refresh();
      return root;
    }
  }
}
