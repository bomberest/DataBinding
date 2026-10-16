namespace JH.DataBinding
{
  /// <summary>Provides the prefab used to display an item in a container binding.</summary>
  public interface IViewProvider
  {
    /// <summary>Returns a View prefab, or null to use the binding's element template.</summary>
    View ViewPrefab { get; }
  }
}
