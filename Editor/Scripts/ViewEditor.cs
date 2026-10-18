using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  [CustomEditor(typeof(View), true)]
  [CanEditMultipleObjects]
  public class ViewEditor : UnityEditor.Editor
  {
    public virtual void OnEnable() { }

    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();
      root.AddToClassList("db-inspector");
      if (DataBindingEditorStyle.StyleSheet != null)
        root.styleSheets.Add(DataBindingEditorStyle.StyleSheet);
      if (serializedObject.isEditingMultipleObjects)
      {
        root.Add(new HelpBox("Select one View to edit model and binding configuration.", HelpBoxMessageType.Info));
        return root;
      }
      var script = new ObjectField("Script")
      {
        objectType = typeof(MonoScript),
        value = MonoScript.FromMonoBehaviour((View)target),
        allowSceneObjects = false,
      };
      script.AddToClassList("db-script");
      script.SetEnabled(false);
      root.Add(script);
      root.Add(new PropertyField(serializedObject.FindProperty(nameof(View.dataSourceType))));
      root.Add(new BindingCollectionElement(serializedObject,
        nameof(View.componentPropertyBindings), "Property Bindings"));
      root.Add(new BindingCollectionElement(serializedObject,
        nameof(View.containerPropertyBindings), "Collection Bindings"));
      var iterator = serializedObject.GetIterator();
      if (iterator.NextVisible(true))
      {
        do
        {
          if (iterator.name == "m_Script" || iterator.name == nameof(View.dataSourceType)
            || iterator.name == nameof(View.componentPropertyBindings)
            || iterator.name == nameof(View.containerPropertyBindings)) continue;
          root.Add(new PropertyField(iterator.Copy()));
        } while (iterator.NextVisible(false));
      }
      return root;
    }
  }
}
