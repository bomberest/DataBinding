using UnityEngine;

namespace JH.DataBinding
{
  /// <summary>Selects a View prefab for each item in a container binding.</summary>
  public abstract class ViewSelector : MonoBehaviour, IViewSelector
  {
    /// <summary>
    /// Returns a prefab, or null to fall back to IViewProvider and then ElementTemplate.
    /// </summary>
    public abstract View SelectView(INotifyDataSourceChanged dataSource);
  }
}
