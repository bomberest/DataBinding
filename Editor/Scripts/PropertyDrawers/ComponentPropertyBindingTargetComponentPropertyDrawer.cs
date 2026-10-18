using UnityEditor;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomPropertyDrawer(typeof(BindingTargetComponentAttribute))]
  public class ComponentPropertyBindingTargetComponentPropertyDrawer : PropertyDrawer
  {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
      BindingInspectorFields.TargetComponent(property.serializedObject.FindProperty(
        property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'))));
  }
}
