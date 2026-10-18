using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JH.DataBinding.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace JH.DataBinding.Editor.Tests
{
  public static class InspectorModels
  {
    public class MainModel : DataSourceBase<MainModel>
    {
      public string State { get; set; }
      public int Value { get; set; }
      public IEnumerable<SlotModel> Items { get; set; }
      public MainModel Nested { get; set; }
      public string this[int index] => index.ToString();
    }
    public class SlotModel : DataSourceBase<SlotModel>, IViewProvider
    {
      public View ViewPrefab => null;
    }
  }
  public class InspectorProbeComponent : MonoBehaviour
  {
    public int Value { get; set; }
  }
  public class InspectorDerivedView : View { public int ExtraValue; }

  public class ViewInspectorTests
  {
    private GameObject obj;
    private View view;
    private SerializedObject serialized;
    private UnityEditor.Editor editor;
    private EditorWindow window;
    private const string ModelPath = "dataSourceType.assemblyQualifiedName";

    [SetUp] public void SetUp()
    {
      obj = new GameObject("Inspector Test");
      view = obj.AddComponent<InspectorDerivedView>();
      view.dataSourceType = new SerializableType();
      serialized = new SerializedObject(view);
    }
    [TearDown] public void TearDown()
    {
      if (window != null) window.Close();
      if (editor != null) Object.DestroyImmediate(editor);
      Object.DestroyImmediate(obj);
      Undo.ClearAll();
    }
    private SerializedProperty PropertyBinding()
    {
      view.dataSourceType.Type = typeof(InspectorModels.MainModel);
      view.componentPropertyBindings = new[] { new ComponentPropertyBinding() };
      serialized.Update();
      return serialized.FindProperty("componentPropertyBindings").GetArrayElementAtIndex(0);
    }
    [TestCase("inspector main", 1)]
    [TestCase("SLOT", 1)]
    [TestCase("not-a-real-type", 0)]
    [TestCase("", 2)]
    public void SearchMatchesAllTermsCaseInsensitively(string query, int expected)
    {
      var types = new[] { typeof(InspectorModels.MainModel), typeof(InspectorModels.SlotModel) };
      Assert.AreEqual(expected, DataSourceTypePicker.FilterTypes(types, query).Count());
    }
    [Test] public void SearchDistinguishesIdenticalClassNamesByNamespace()
    {
      var types = new[] { typeof(PickerAlpha.Model), typeof(PickerBeta.Model) };
      Assert.AreEqual(2, DataSourceTypePicker.FilterTypes(types, "model").Count());
      Assert.AreEqual(typeof(PickerBeta.Model), DataSourceTypePicker.FilterTypes(types, "pickerbeta").Single());
      StringAssert.Contains("PickerBeta", DataSourceTypePicker.Context(typeof(PickerBeta.Model)));
    }
    [Test] public void SearchCanMatchAssemblyName()
    {
      Assert.AreEqual(1, DataSourceTypePicker.FilterTypes(new[] { typeof(InspectorModels.MainModel) },
        typeof(InspectorModels.MainModel).Assembly.GetName().Name).Count());
    }
    [UnityTest] public IEnumerator PickerSearchDoesNotSelectUntilEnter()
    {
      if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        Assert.Ignore("Popup interaction validation requires a graphics device.");
      Type chosen = null;
      DataSourceTypePicker.Show(new Rect(120, 120, 200, 22), typeof(InspectorModels.MainModel),
        type => chosen = type);
      window = Resources.FindObjectsOfTypeAll<DataSourceTypePicker>().Single();
      yield return null;
      yield return null;
      var search = window.rootVisualElement.Q<ToolbarSearchField>("modelSearch");
      Assert.NotNull(search);
      search.value = "inspector slot";
      Assert.IsNull(chosen);
      var list = window.rootVisualElement.Q<ListView>("modelResults");
      Assert.AreEqual(typeof(InspectorModels.SlotModel), list.itemsSource[0]);
      using (var evt = KeyDownEvent.GetPooled(new Event
        { type = EventType.KeyDown, keyCode = KeyCode.Return }))
        search.SendEvent(evt);
      yield return null;
      Assert.AreEqual(typeof(InspectorModels.SlotModel), chosen);
    }
    [Test] public void OpeningInspectorDoesNotGuessOrMutateModel()
    {
      editor = UnityEditor.Editor.CreateEditor(view);
      var root = ((ViewEditor)editor).CreateInspectorGUI();
      Assert.IsNull(view.dataSourceType.Type);
      Assert.NotNull(root.Query<PropertyField>().ToList().FirstOrDefault(x => x.bindingPath == "ExtraValue"));
      Assert.AreEqual(2, root.Query<BindingCollectionElement>().ToList().Count);
    }
    [Test] public void ModelSelectionIsUndoableAndKeepsBindings()
    {
      PropertyBinding().FindPropertyRelative("sourcePath").stringValue = "Value";
      serialized.ApplyModifiedPropertiesWithoutUndo();
      Undo.IncrementCurrentGroup();
      DataSourceTypePropertyDrawer.SetModelType(serialized, ModelPath, typeof(InspectorModels.SlotModel));
      Assert.AreEqual(typeof(InspectorModels.SlotModel), view.dataSourceType.Type);
      Undo.PerformUndo();
      serialized.Update();
      Assert.AreEqual(typeof(InspectorModels.MainModel), view.dataSourceType.Type);
      Assert.AreEqual("Value", view.componentPropertyBindings[0].SourcePath);
    }
    [Test] public void ClearingAndMissingModelRenderWithoutExceptions()
    {
      var property = PropertyBinding();
      DataSourceTypePropertyDrawer.SetModelType(serialized, ModelPath, null);
      Assert.DoesNotThrow(() => new ComponentPropertyBindingPropertyDrawer().CreatePropertyGUI(property));
      Assert.DoesNotThrow(() => new ComponentPropertyBindingSourcePathPropertyDrawer()
        .CreatePropertyGUI(property.FindPropertyRelative("sourcePath")));
      serialized.FindProperty(ModelPath).stringValue = "No.Such.Type, No.Such.Assembly";
      serialized.ApplyModifiedProperties();
      Assert.DoesNotThrow(() => new DataSourceTypePropertyDrawer()
        .CreatePropertyGUI(serialized.FindProperty("dataSourceType")));
    }
    [Test] public void CollectionChoicesExcludeStringsAndIndexers()
    {
      var all = DataBindingCommonData.GetBindableDataSourceProperties(typeof(InspectorModels.MainModel));
      CollectionAssert.DoesNotContain(all.Select(x => x.Name), "Item");
      var collections = DataBindingCommonData.GetBindableDataSourceEnumerableProperties(typeof(InspectorModels.MainModel));
      CollectionAssert.AreEqual(new[] { "Items" }, collections.Select(x => x.Name));
    }
    [Test] public void ReadOnlyTargetsAreNotOfferedOrMarkedComplete()
    {
      var binding = new ComponentPropertyBinding
      { SourcePath = "Value", TargetComponent = obj.transform, TargetPath = "childCount" };
      Assert.AreNotEqual(ComponentPropertyBindingState.Complete,
        DataBindingCommonData.DetermineComponentPropertyBindingState(binding, typeof(InspectorModels.MainModel)).bindingState);
      CollectionAssert.DoesNotContain(DataBindingCommonData.GetBindableComponentProperties(obj.transform, typeof(int))
        .Select(x => x.Name), "childCount");
    }
    [UnityTest] public IEnumerator DuplicateComponentNamesSelectTheCorrectInstance()
    {
      if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        Assert.Ignore("Interactive field validation requires a graphics device.");
      var first = obj.AddComponent<InspectorProbeComponent>();
      var second = obj.AddComponent<InspectorProbeComponent>();
      var property = PropertyBinding();
      property.FindPropertyRelative("sourcePath").stringValue = "Value";
      property.FindPropertyRelative("targetGameObject").objectReferenceValue = obj;
      serialized.ApplyModifiedProperties();
      var field = (DropdownField)BindingInspectorFields.TargetComponent(property);
      var firstLabel = BindingInspectorFields.ComponentLabel(first, obj.transform);
      var secondLabel = BindingInspectorFields.ComponentLabel(second, obj.transform);
      Assert.AreNotEqual(firstLabel, secondLabel);
      window = ScriptableObject.CreateInstance<EditorWindow>();
      window.Show();
      window.rootVisualElement.Add(field);
      yield return null;
      field.value = secondLabel;
      Assert.AreSame(second, view.componentPropertyBindings[0].TargetComponent);
    }
    [UnityTest] public IEnumerator EmptyListsStayCompactAndAddingCreatesBlankBinding()
    {
      if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        Assert.Ignore("Window layout validation requires a graphics device.");
      editor = UnityEditor.Editor.CreateEditor(view);
      var root = ((ViewEditor)editor).CreateInspectorGUI();
      window = ScriptableObject.CreateInstance<EditorWindow>();
      window.position = new Rect(100, 100, 380, 450);
      window.Show();
      window.rootVisualElement.Add(root);
      root.Bind(serialized);
      yield return null;
      yield return null;
      foreach (var section in root.Query<BindingCollectionElement>().ToList())
        Assert.Less(section.worldBound.height, 130f, "Empty binding section must not reserve a large list viewport");
      root.Query<BindingCollectionElement>().ToList().First().AddBinding();
      yield return null;
      Assert.AreEqual(1, view.componentPropertyBindings.Length);
      Assert.IsTrue(string.IsNullOrEmpty(view.componentPropertyBindings[0].SourcePath));
      Assert.IsNull(view.componentPropertyBindings[0].TargetComponent);
    }
    [UnityTest] public IEnumerator PopulatedInspectorCreatesBothBindingTypesAndSupportsRemoveUndo()
    {
      if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        Assert.Ignore("Window layout validation requires a graphics device.");
      var property = PropertyBinding();
      property.FindPropertyRelative("sourcePath").stringValue = "Value";
      serialized.ApplyModifiedProperties();
      view.containerPropertyBindings = new[] { new ContainerPropertyBinding { SourcePath = "Items", TargetContainer = obj.transform } };
      serialized.Update();
      foreach (var name in new[] { "componentPropertyBindings", "containerPropertyBindings" })
        serialized.FindProperty(name).GetArrayElementAtIndex(0).isExpanded = true;
      editor = UnityEditor.Editor.CreateEditor(view);
      var root = ((ViewEditor)editor).CreateInspectorGUI();
      window = ScriptableObject.CreateInstance<EditorWindow>();
      window.position = new Rect(100, 100, 400, 650);
      window.Show();
      window.rootVisualElement.Add(root);
      root.Bind(serialized);
      yield return null;
      yield return null;
      Assert.AreEqual(2, root.Query<ToolbarMenu>().ToList().Count);
      Assert.IsTrue(root.Query<Label>().ToList().Any(x => x.text == "View Selection"));
      Assert.IsTrue(root.Query<Label>().ToList().Any(x => x.text == "Selector → Model → Default Prefab"));
      Undo.IncrementCurrentGroup();
      var menu = root.Query<ToolbarMenu>().ToList().First();
      menu.menu.MenuItems().OfType<DropdownMenuAction>().Single(x => x.name == "Remove").Execute();
      Assert.AreEqual(0, view.componentPropertyBindings.Length);
      Undo.PerformUndo();
      yield return null;
      Assert.AreEqual(1, view.componentPropertyBindings.Length);
      Assert.AreEqual("Value", view.componentPropertyBindings[0].SourcePath);
      LogAssert.NoUnexpectedReceived();
    }
  }
}
namespace PickerAlpha { public class Model : JH.DataBinding.DataSourceBase<Model> { } }
namespace PickerBeta { public class Model : JH.DataBinding.DataSourceBase<Model> { } }
