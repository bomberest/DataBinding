using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  internal class BindingCollectionElement : VisualElement
  {
    private readonly SerializedObject serialized;
    private readonly string path;
    private readonly string title;
    private readonly Foldout section;
    private readonly VisualElement rows;
    private int lastCount = -1;

    internal BindingCollectionElement(SerializedObject serialized, string path, string title)
    {
      this.serialized = serialized;
      this.path = path;
      this.title = title;
      AddToClassList("db-collection");
      section = new Foldout { value = true, viewDataKey = path + "-section" };
      section.AddToClassList("db-collection-foldout");
      Add(section);
      rows = new VisualElement();
      section.Add(rows);
      var add = new Button(AddBinding)
        { text = "+", name = "add-" + path, tooltip = "Add binding" };
      add.AddToClassList("db-add-binding");
      Add(add);
      this.TrackSerializedObjectValue(serialized, _ =>
      {
        if (serialized.targetObject != null && serialized.FindProperty(path).arraySize != lastCount)
          Rebuild();
      });
      Rebuild();
    }

    internal void AddBinding()
    {
      serialized.Update();
      var array = serialized.FindProperty(path);
      var index = array.arraySize++;
      var item = array.GetArrayElementAtIndex(index);
      item.boxedValue = path == nameof(View.componentPropertyBindings)
        ? (object)new ComponentPropertyBinding() : new ContainerPropertyBinding();
      item.isExpanded = true;
      serialized.ApplyModifiedProperties();
      section.value = true;
      Rebuild();
    }

    internal void Rebuild()
    {
      rows.Clear();
      var array = serialized.FindProperty(path);
      lastCount = array.arraySize;
      section.text = $"{title} ({lastCount})";
      if (lastCount == 0)
      {
        var empty = new Label("No bindings");
        empty.AddToClassList("db-empty");
        rows.Add(empty);
        return;
      }
      for (var index = 0; index < lastCount; index++)
      {
        var row = new VisualElement();
        row.AddToClassList("db-binding-row");
        var property = new PropertyField(array.GetArrayElementAtIndex(index));
        property.AddToClassList("db-binding-property");
        property.BindProperty(array.GetArrayElementAtIndex(index));
        row.Add(property);
        var actions = new ToolbarMenu { text = "⋮", tooltip = "Move, duplicate or remove binding" };
        actions.AddToClassList("db-binding-actions");
        actions.Q(className: ToolbarMenu.arrowUssClassName)?.AddToClassList("db-hidden");
        var itemIndex = index;
        actions.menu.AppendAction("Move Up", _ => Move(itemIndex, itemIndex - 1),
          itemIndex > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        actions.menu.AppendAction("Move Down", _ => Move(itemIndex, itemIndex + 1),
          itemIndex + 1 < lastCount ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        actions.menu.AppendAction("Duplicate", _ =>
        {
          serialized.Update();
          serialized.FindProperty(path).InsertArrayElementAtIndex(itemIndex);
          serialized.ApplyModifiedProperties();
          Rebuild();
        });
        actions.menu.AppendSeparator();
        actions.menu.AppendAction("Remove", _ =>
        {
          serialized.Update();
          serialized.FindProperty(path).DeleteArrayElementAtIndex(itemIndex);
          serialized.ApplyModifiedProperties();
          Rebuild();
        });
        row.Add(actions);
        rows.Add(row);
      }
    }

    private void Move(int from, int to)
    {
      serialized.Update();
      serialized.FindProperty(path).MoveArrayElement(from, to);
      serialized.ApplyModifiedProperties();
      Rebuild();
    }
  }
}
