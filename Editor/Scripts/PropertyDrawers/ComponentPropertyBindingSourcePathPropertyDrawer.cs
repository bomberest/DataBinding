using UnityEditor;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomPropertyDrawer(typeof(BindingSourcePathAttribute))]
  public class ComponentPropertyBindingSourcePathPropertyDrawer : PropertyDrawer
  {
    public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
      BindingInspectorFields.Source(property, false);
  }
}
