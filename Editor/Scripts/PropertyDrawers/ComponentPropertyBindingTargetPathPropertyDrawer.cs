using UnityEditor;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomPropertyDrawer(typeof(BindingTargetPathAttribute))]
  public class ComponentPropertyBindingTargetPathPropertyDrawer : PropertyDrawer
  {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
      BindingInspectorFields.TargetPath(property.serializedObject.FindProperty(
        property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'))));
  }
}
