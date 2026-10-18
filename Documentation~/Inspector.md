# View Inspector

The inspector has a model picker, **Property Bindings**, and **Collection Bindings**.
Empty binding sections stay compact. Extra serialized fields on derived `View`
components are shown below these sections.

## Model selection

Click **Model** to open a searchable picker. Search matches class names, namespace
and assembly; space-separated words must all match. For example, `inventory main`
finds `ItemsInventoryWidget.MainModel` without scrolling through unrelated classes.

Each result shows its class name and a second line with namespace and assembly.
Classes with the same name are separate choices. Use Up/Down and Enter, double-click
a result, or press **Select**. Escape or **Cancel** leaves the model unchanged.
**Clear** removes the selected type. Searching does not change the model.

**Open** opens the selected model's source when it can be identified, including
nested models declared inside a component. **New** creates a model source file.
Opening the inspector no longer guesses and assigns a model automatically.
Selecting and clearing the model support Undo.

## Property bindings

Headers summarize the connection, for example `State → [Fsm].State`.
Choose **Source**, **Target Object**, **Component**, and **Property**. Components
are identified by type and hierarchy path; duplicate components have an index.
Inactive descendants are included. Target choices contain compatible public setters.
Incomplete bindings show a configuration hint.

## Collection bindings

Headers summarize the list and parent, for example `Items → Content`.
Choose **Items Source** and **Container**, then configure **View Selection**:

1. **Selector**: an optional `ViewSelector` component.
2. The item's `IViewProvider.ViewPrefab`.
3. **Default Prefab**: the existing `ElementTemplate` fallback.

Null results fall through to the next option. The inspector displays this priority.
Dynamic prefab selection is evaluated at runtime; a configured binding does not
guarantee that every future model will provide a prefab.

Use **+ Add Binding** to create a blank entry. Each entry's menu offers **Move Up**,
**Move Down**, **Duplicate**, and **Remove**. These operations support Undo.

The updated labels do not rename serialized fields or change runtime binding APIs.
Older tutorial screenshots may show the previous labels and dropdown.
