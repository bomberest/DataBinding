using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JH.DataBinding
{
  [Serializable]
  public class ContainerPropertyBinding
  {
    [SerializeField]
    [EnumerableBindingSourcePath]
    private string sourcePath;

    [SerializeField]
    private Transform targetContainer;

    [SerializeField]
    private View elementTemplate;

    [SerializeField]
    private ViewSelector viewSelector;

    private readonly List<(View Instance, View Template)> childViews = new();

    private object dataSource;

    private MethodInfo[] dataSourcePropertyAccessors;

    public object DataSource
    {
      get => dataSource;
      set
      {
        dataSource = value;
        BindSource();
      }
    }

    public string SourcePath
    {
      get => sourcePath;
      set
      {
        sourcePath = value;
        BindSource();
      }
    }

    public Transform TargetContainer
    {
      get => targetContainer;
      set => targetContainer = value;
    }

    public View ElementTemplate
    {
      get => elementTemplate;
      set => elementTemplate = value;
    }

    public ViewSelector ViewSelector
    {
      get => viewSelector;
      set => viewSelector = value;
    }

    private void BindSource()
    {
      if (dataSource != null && !String.IsNullOrEmpty(sourcePath))
      {
        dataSourcePropertyAccessors = dataSource
          .ResolvePublicPropertyPath(SourcePath, PathResolveOperation.GetValue)
          .ToArray();
      }
      else
      {
        dataSourcePropertyAccessors = Array.Empty<MethodInfo>();
      }
    }

    public void UpdateBinding()
    {
      if (
        (dataSourcePropertyAccessors != null)
        && (TargetContainer != null)
      )
      {
        if (dataSource is null)
        {
          RemoveSuperfluousChildren(0);
          return;
        }

        var boundInstances = (
          (
            dataSourcePropertyAccessors.InvokeGetAccessChain(dataSource)
            as IEnumerable<INotifyDataSourceChanged>
          ) ?? Array.Empty<INotifyDataSourceChanged>()
        ).ToArray();

        var templates = new View[boundInstances.Length];
        for (var index = 0; index < boundInstances.Length; index++)
        {
          templates[index] = ResolveTemplate(boundInstances[index]);
          if (templates[index] == null)
          {
            Debug.LogError(
              $"No View prefab could be resolved for item {index} in '{SourcePath}'. "
                + "Assign a ViewSelector, provide IViewProvider.ViewPrefab, or set ElementTemplate.",
              TargetContainer
            );
            return;
          }
        }

        RemoveSuperfluousChildren(boundInstances.Length);

        for (var childIndex = 0; childIndex < boundInstances.Length; childIndex++)
        {
          var view = GetOrCreateChild(childIndex, templates[childIndex], boundInstances[childIndex]);
          view.DataSource = boundInstances[childIndex];
        }
      }
    }

    private View ResolveTemplate(INotifyDataSourceChanged item)
    {
      var selectedView = ViewSelector != null ? ViewSelector.SelectView(item) : null;
      if (selectedView != null)
      {
        return selectedView;
      }

      var providedView = (item as IViewProvider)?.ViewPrefab;
      return providedView != null ? providedView : ElementTemplate;
    }

    private View GetOrCreateChild(int index, View template, INotifyDataSourceChanged item)
    {
      View view = null;
      if (index < TargetContainer.childCount)
      {
        var child = TargetContainer.GetChild(index);
        view = child.GetComponent<View>();
        var tracked = index < childViews.Count && childViews[index].Instance == view;
        var canReuse = view != null && (
          (tracked && childViews[index].Template == template)
          // Keep supporting pre-populated containers with the original fixed template.
          || (!tracked && ViewSelector == null && !(item is IViewProvider))
        );

        if (!canReuse)
        {
          RemoveChild(child);
          view = null;
        }
      }

      if (view == null)
      {
        view = UnityEngine.Object.Instantiate(template, TargetContainer);
        view.transform.SetSiblingIndex(index);
      }

      if (index < childViews.Count)
      {
        childViews[index] = (view, template);
      }
      else
      {
        childViews.Add((view, template));
      }

      return view;
    }

    private void RemoveSuperfluousChildren(int numberOfChildrenToKeep)
    {
      while (TargetContainer.childCount > numberOfChildrenToKeep)
      {
        RemoveChild(TargetContainer.GetChild(TargetContainer.childCount - 1));
      }

      if (childViews.Count > numberOfChildrenToKeep)
      {
        childViews.RemoveRange(numberOfChildrenToKeep, childViews.Count - numberOfChildrenToKeep);
      }
    }

    private static void RemoveChild(Transform child)
    {
      var childView = child.GetComponent<View>();
      child.gameObject.SetActive(false);
      // Detach before clearing DataSource so View does not subscribe to its parent's source.
      child.SetParent(null);
      if (childView != null)
      {
        childView.DataSource = null;
      }

      UnityEngine.Object.Destroy(child.gameObject);
    }
  }
}
