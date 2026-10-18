# A look at ContainerBindings

This is a short step by step tutorial that will make you familiar with container bindings.

We assume you completed the "Getting Started" tutorial and have a basic understanding of what data bindings are.

## Open the tutorial project

- Open up Unity.
- Import the DataBinding package if you have not done this already. You can follow the instructions in the ["Getting Started" tutorial](../README.md) to do so.
- Open the PackageManager Window ( Menu: Window ⟶ Package Manager ) and select the DataBinding package.
- From the samples on the right select and import **"A look at ContainerBindings - Tutorial"**.
- Back in unity open the "A look at ContainerBindings" scene.
- Running the sample renders an empty screen. But we will make it look like this:
  
  ![Tutorial Start](Images/ALookAtContainerBindings/ALookAtContainerBindings.Final.png)

  with just 2 small setups.
  
## Binding the Party View to display Characters
- The Project already contains some code in Bootstrap.cs that generates random data for 4 characters. The data is exposed by the ParyViewModel's Charcters property.

    <details>
    <summary>Show code</summary>
    
    ```csharp
    using System.Collections.Generic;

    namespace JH.DataBinding.Examples.ContainerBindings
    {
        public class PartyViewModel : DataSourceBase<PartyViewModel>
        {
            public IEnumerable<CharacterViewModel> Characters { get; set; }
        }
    }

    ```
    </details>
    
- What we are trying to achieve, is to display character data in the Party View. So lets open the **Party View** prefab. (*Hint: Typing 'Party V' in the search bar of our Project Window can help you find it faster*)
  
  ![PartyViewSetup](Images/ALookAtContainerBindings/PartyViewSetup.01.png)

- Now add a new Binding by clicking the **Add Binding** button in the **Collection Bindings** section of your Inspector window.
- Select the **Items Source** property to bind to the Characters property of your PartyViewModel. The editor will limit your choices to **IEnumerable** properties only. After all: This is a Container, so we expect it to hold Elements in a 1:n relationship. If we were to bind a 1:1 relationship, a Component Property Binding would do.
- As **Container** select the Characters GameObject from your Hierarchy (It is already set up to show a Grid for our purpose).
- Now finally, as we are binding 1:n, we have to supply an **Default Prefab** that will be used to represent each element of the IEnumerable property in the scene. To do this, drag the **Character View** prefab into the **Default Prefab** binding input area. *(Hint: You can find the **Character View** prefab right next to the **Party View** prefab that you are current editing. If not you maybe have the Party V filter still active.*)

- If you click run now you should be a scene like this:

    ![PartyViewSetup - Run](Images/ALookAtContainerBindings/PartyViewSetup.02.png)

- Note that the **Character View** prefab itself is a **View** with it's own property bindings that become evaluated for each of the **CharacterViewModel**s that are stored in the **Characters** Property of the **PartyViewModel**.

    ![CharacterViewSetup - Run](Images/ALookAtContainerBindings/CharacterViewSetup.01.png)

## Binding the Charcter View to display Attributes

Attributes are for Characters pretty much what Characters are for the Party. There is a 1:n relation of Characters:Attributes. Characters *'contain'* Attributes. So we go create a ContainerBinding:

  ![CharacterViewSetup - Run](Images/ALookAtContainerBindings/CharacterViewSetup.02.png)

- Select the **Attributes** property of the CharacterViewModel as **Items Source** of the Binding.
- Drag the **Attributes** GameObject into the **Container** inputArea. (*Hint: Just as with **Character View**s the **Attribute** GameObject is properly set up as Grid already.*)
- Drag the **Attribute View** prefab into the **Default Prefab** input are.
- Click Play and you should see the final result.

## Choosing a prefab for each item

Container bindings resolve a `View` prefab in this order:

1. `ViewSelector.SelectView(item)`, when a selector is assigned to the binding.
2. `IViewProvider.ViewPrefab`, when the item implements `IViewProvider`.
3. The binding's existing `ElementTemplate`.

A null result falls through to the next option. `ElementTemplate` can be left empty
if the selector or providers supply a prefab for every item. If none supplies a
prefab, the binding logs an error and leaves the container unchanged for that update.
These APIs return prefab references; the binding creates and destroys the instances.

Implement `IViewProvider` on an item model to provide its prefab directly:

```csharp
using JH.DataBinding;

public class CharacterItem : DataSourceBase<CharacterItem>, IViewProvider
{
    public View ViewPrefab { get; set; }
    public string Name { get; set; }
}
```

For selection configured independently of the model, derive from `ViewSelector`
(`MonoBehaviour`, implementing `IViewSelector`) and add your component to a
GameObject. Assign that component to the container binding's **View Selector**
field in the Inspector:

```csharp
using JH.DataBinding;
using UnityEngine;

public class CharacterViewSelector : ViewSelector
{
    [SerializeField] private View characterPrefab;

    public override View SelectView(INotifyDataSourceChanged dataSource)
    {
        return dataSource is CharacterItem ? characterPrefab : null;
    }
}
```

The selector takes priority even when an item implements `IViewProvider`. The
Inspector recognizes provider-only bindings when the declared collection element
type implements `IViewProvider`; for heterogeneous collections declared using a
base type, assign a selector or fallback template for editor validation.

Each update reuses child views whose resolved prefab has not changed and replaces
views whose prefab has changed, keeping list order. Pre-populated children remain
supported for fixed-template bindings; dynamic selection replaces untracked children
because their original prefab is unknown. After changing an item's prefab or selector
inputs, notify the parent data source with `NotifyChanges(...)` to update the container.
The container does not subscribe to individual item notifications.
