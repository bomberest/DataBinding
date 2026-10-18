using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace JH.DataBinding.Editor
{
  internal class DataSourceTypePicker : EditorWindow
  {
    private Type[] types;
    private Type selectedType;
    private Action<Type> choose;
    private ListView results;
    private Label count;
    private Label empty;
    private Button confirm;

    internal static IEnumerable<Type> FilterTypes(IEnumerable<Type> source, string query)
    {
      var tokens = (query ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      return source.Where(type => tokens.All(token =>
        $"{type.GetFriendlyName()} {type.FullName} {type.Assembly.GetName().Name}"
          .IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    internal static string Context(Type type) =>
      $"{type.Namespace ?? "Global namespace"}  ·  {type.Assembly.GetName().Name}";

    internal static void Show(Rect anchor, Type currentType, Action<Type> onChoose)
    {
      var window = CreateInstance<DataSourceTypePicker>();
      window.titleContent = new GUIContent("Choose Model");
      window.types = DataBindingCommonData.GetValidDataSourceTypes();
      window.selectedType = currentType;
      window.choose = onChoose;
      window.ShowAsDropDown(GUIUtility.GUIToScreenRect(anchor), new Vector2(440, 390));
    }

    public void CreateGUI()
    {
      var root = rootVisualElement;
      if (DataBindingEditorStyle.StyleSheet != null)
        root.styleSheets.Add(DataBindingEditorStyle.StyleSheet);
      root.AddToClassList("db-type-picker");
      var title = new Label("Choose Model");
      title.AddToClassList("db-section-title");
      root.Add(title);
      var search = new ToolbarSearchField
      {
        name = "modelSearch",
        tooltip = "Search class, namespace or assembly. Space separates search terms.",
      };
      search.AddToClassList("db-type-search");
      root.Add(search);
      count = new Label();
      count.AddToClassList("db-secondary");
      root.Add(count);
      results = new ListView
      {
        name = "modelResults",
        fixedItemHeight = 46,
        virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
        selectionType = SelectionType.Single,
        makeItem = MakeRow,
        bindItem = BindRow,
      };
      results.AddToClassList("db-type-results");
      results.selectionChanged += selection =>
      {
        selectedType = selection.OfType<Type>().FirstOrDefault();
        confirm?.SetEnabled(selectedType != null);
      };
      results.itemsChosen += selection => Select(selection.OfType<Type>().FirstOrDefault());
      root.Add(results);
      empty = new Label("No matching models");
      empty.AddToClassList("db-picker-empty");
      root.Add(empty);
      var footer = new VisualElement();
      footer.AddToClassList("db-picker-footer");
      footer.Add(new Button(() => Select(null)) { text = "Clear", tooltip = "Remove the model type" });
      var spacer = new VisualElement();
      spacer.AddToClassList("db-flex");
      footer.Add(spacer);
      footer.Add(new Button(Close) { text = "Cancel" });
      confirm = new Button(() => Select(selectedType)) { text = "Select" };
      footer.Add(confirm);
      root.Add(footer);
      search.RegisterValueChangedCallback(evt => Refresh(evt.newValue));
      root.RegisterCallback<KeyDownEvent>(evt =>
      {
        if (evt.keyCode == KeyCode.Escape) Close();
        else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
          if (selectedType != null) Select(selectedType);
        }
        else if (evt.target is VisualElement element && (element == search || search.Contains(element)))
        {
          var delta = evt.keyCode == KeyCode.DownArrow ? 1 : evt.keyCode == KeyCode.UpArrow ? -1 : 0;
          if (delta == 0) return;
          if (results.itemsSource.Count > 0)
          {
            var next = Mathf.Clamp(results.selectedIndex + delta, 0, results.itemsSource.Count - 1);
            results.SetSelection(next);
            results.ScrollToItem(next);
          }
        }
        else return;
        evt.StopPropagation();
      }, TrickleDown.TrickleDown);
      Refresh("");
      root.schedule.Execute(() => search.Focus());
    }

    private VisualElement MakeRow()
    {
      var row = new VisualElement();
      row.AddToClassList("db-type-option");
      row.Add(new Label { name = "typeName" });
      var context = new Label { name = "typeContext" };
      context.AddToClassList("db-secondary");
      row.Add(context);
      return row;
    }

    private void BindRow(VisualElement row, int index)
    {
      var type = (Type)results.itemsSource[index];
      row.Q<Label>("typeName").text = type.GetFriendlyName();
      row.Q<Label>("typeContext").text = Context(type);
      row.tooltip = $"{type.FullName}\n{type.Assembly.GetName().Name}";
    }

    private void Refresh(string query)
    {
      var previous = selectedType;
      var filtered = FilterTypes(types ?? Array.Empty<Type>(), query).ToList();
      results.itemsSource = filtered;
      results.Rebuild();
      var index = filtered.IndexOf(previous);
      results.SetSelection(index >= 0 ? index : filtered.Count > 0 ? 0 : -1);
      selectedType = results.selectedIndex >= 0 ? filtered[results.selectedIndex] : null;
      confirm.SetEnabled(selectedType != null);
      count.text = $"{filtered.Count} of {types?.Length ?? 0} models";
      empty.EnableInClassList("db-hidden", filtered.Count > 0);
    }

    private void Select(Type type)
    {
      choose?.Invoke(type);
      Close();
    }
  }
}
