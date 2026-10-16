namespace JH.DataBinding
{
  /// <summary>Selects a View prefab for an item in a container binding.</summary>
  public interface IViewSelector
  {
    /// <summary>
    /// Returns a prefab, or null to fall back to IViewProvider and then ElementTemplate.
    /// </summary>
    View SelectView(INotifyDataSourceChanged dataSource);
  }
}
