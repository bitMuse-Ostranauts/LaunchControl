# MegaTooltip Module Registration System

## Overview

LaunchControl provides a powerful system for registering custom data modules that integrate seamlessly with Ostranauts' MegaToolTip system. This allows mods to add custom UI elements to the tooltips that appear when right-clicking on items, containers, or crew members.

The system uses a fluent API for easy configuration, handles GameObject template creation and caching automatically, and integrates with the existing MegaToolTip lifecycle.

## Key Features

- **Fluent Registration API**: Chain configuration methods for clean, readable code
- **Automatic Template Management**: GameObjects are created once and cached for efficient cloning
- **Conditional Display**: Show modules only when specific conditions are met
- **Flexible Positioning**: Insert modules before or after existing vanilla modules
- **Separate Item/Person Registration**: Register different modules for items vs. crew/persons
- **.NET Framework Compatible**: Uses custom delegate types for compatibility with older Unity/Mono versions

## Quick Start

### Basic Registration

```csharp
using Ostranauts.Bit;
using Ostranauts.Bit.SmarterHauling.UI;

// In your plugin's Awake() method:
LaunchControl.RegisterItemModule<MyCustomModule>(MyCustomModule.SetupUI)
    .OnlyShowIf(co => co.GetContainer() != null)
    .InsertAfter("ValueModule");
```

### Creating a Custom Module

1. Create a class that extends `ModuleBase` (or implements `IDataModule`)
2. Implement the `SetData(CondOwner co)` method
3. Create a static `SetupUI(GameObject go)` method to build the UI

```csharp
using Ostranauts.UI.MegaToolTip.DataModules;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyCustomModule : ModuleBase
{
    private TextMeshProUGUI _text;

    public override void SetData(CondOwner co)
    {
        if (co == null)
        {
            _IsMarkedForDestroy = true;
            return;
        }

        // Your logic to determine if this module should display
        if (!ShouldShowForThisCondOwner(co))
        {
            _IsMarkedForDestroy = true;
            return;
        }

        // Find UI components (cached from template)
        if (_text == null)
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Update UI with data from CondOwner
        _text.text = $"Custom Info: {co.ShortName}";

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public static void SetupUI(GameObject go)
    {
        // Add background
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.278f);

        // Add layout
        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 5, 5);

        // Setup RectTransform
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 32);

        // Add text element
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 12;
        text.color = Color.white;
    }

    private bool ShouldShowForThisCondOwner(CondOwner co)
    {
        // Your custom logic here
        return true;
    }
}
```

## Module Lifecycle

### 1. Registration Phase (Plugin Startup)

When you call `LaunchControl.RegisterItemModule<T>()`, LaunchControl:
- Creates a registration record with your module type and setup callback
- Does NOT create any GameObjects yet
- Returns a `ModuleRegistration` for fluent configuration

### 2. Template Creation (First Use)

When the first tooltip needs your module, LaunchControl:
- Creates a new GameObject
- Adds your module component
- Calls your `SetupUI(GameObject)` method to build the UI
- Stores the GameObject as an inactive template for future cloning

### 3. Module Instantiation (Each Tooltip Display)

When a tooltip is shown, LaunchControl's patch:
- Checks if your module's visibility predicate passes
- Clones the template GameObject
- Parents it to the tooltip's module container
- Calls `SetData(CondOwner)` on your module
- Checks `IsMarkedForDestroy()` and removes if true
- Inserts into the module list at the correct position

### 4. Module Update (Periodic)

The vanilla `ModuleHost` periodically calls `UpdateUI` event:
- Your module can subscribe in `Awake()` via `ModuleHost.UpdateUI.AddListener()`
- Use this to refresh dynamic content

### 5. Module Cleanup (Tooltip Close/Change)

When the tooltip closes or switches to a new object:
- The vanilla system calls `Destroy()` on all modules
- GameObjects are destroyed
- Templates remain cached for next use

## API Reference

### Custom Delegate Types

LaunchControl defines custom delegate types for compatibility with older .NET Framework versions used by Unity:

```csharp
// Delegate for visibility predicates
public delegate bool VisibilityPredicate(CondOwner co);

// Delegate for UI setup callbacks
public delegate void UISetupCallback(GameObject go);
```

These are used instead of `System.Func<T1, T2>` and `System.Action<T>` which aren't available in the older mscorlib version.

### Registration Methods

#### `LaunchControl.RegisterItemModule<T>(UISetupCallback setupUI)`

Register a module for item tooltips.

**Parameters:**
- `T`: Your module type (must implement `IDataModule` and extend `MonoBehaviour`)
- `setupUI`: Static method to create the UI structure

**Returns:** `ModuleRegistration` for fluent configuration

**Example:**
```csharp
LaunchControl.RegisterItemModule<StorageSettingsModule>(StorageSettingsModule.SetupUI)
    .OnlyShowIf(co => co.GetContainer() != null)
    .InsertAfter("ValueModule");
```

#### `LaunchControl.RegisterPersonModule<T>(UISetupCallback setupUI)`

Register a module for person/crew tooltips.

**Parameters:**
- Same as `RegisterItemModule`

**Returns:** `ModuleRegistration` for fluent configuration

### Fluent Configuration Methods

#### `.OnlyShowIf(VisibilityPredicate predicate)`

Set a condition for when this module should be displayed.

**Parameters:**
- `predicate`: Delegate that receives a `CondOwner` and returns `true` to show the module (`delegate bool VisibilityPredicate(CondOwner co)`)

**Returns:** `ModuleRegistration` for chaining

**Example:**
```csharp
.OnlyShowIf(co => co.GetContainer() != null && co.GetContainer().GetCapacity() > 0)
```

If no predicate is set, the module will always be created and `SetData` will be called. Use `_IsMarkedForDestroy = true` in `SetData` for more complex logic.

#### `.InsertAfter(string moduleName)`

Insert this module after a specific vanilla module.

**Parameters:**
- `moduleName`: The class name of the target module (e.g., "ValueModule", "CondsModule")

**Returns:** `ModuleRegistration` for chaining

**Common Module Names:**
- `ValueModule` - Shows item price
- `CondsModule` - Shows conditions/stats
- `ItemModule` - Shows item details
- `PersonModule` - Shows person details
- `StatusModule` - Shows status effects
- `ToggleMoreModule` - The "Show More" button

**Example:**
```csharp
.InsertAfter("ValueModule")
```

#### `.InsertBefore(string moduleName)`

Insert this module before a specific vanilla module.

**Parameters:**
- `moduleName`: The class name of the target module

**Returns:** `ModuleRegistration` for chaining

**Example:**
```csharp
.InsertBefore("ToggleMoreModule")
```

If the target module is not found, the module will be appended to the end.

## UI Setup Patterns

### Adding Sprites

Use LaunchControl's `SpriteUtility` to find sprites by keywords:

```csharp
Sprite sprite = SpriteUtility.FindSpriteByKeywords(new[] { "gradient", "bordered" });
if (sprite != null)
{
    image.sprite = sprite;
}
```

### Layout Recommendations

Match the vanilla module style:
- Background: Semi-transparent black `new Color(0f, 0f, 0f, 0.278f)`
- Height: 32 pixels for single-line modules
- Padding: 5px on all sides
- Use `HorizontalLayoutGroup` for horizontal layouts
- Use `RectTransform` anchoring for proper sizing

### Text Style

For consistency with vanilla:
- Font: TextMeshProUGUI (TMPro)
- Size: 10-12 for body text
- Color: White or light colors
- Bold for emphasis: `FontStyles.Bold`

### Buttons

Match vanilla button style:
```csharp
Button button = buttonObj.AddComponent<Button>();
ColorBlock colors = button.colors;
colors.normalColor = Color.white;
colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
button.colors = colors;
```

## Complete Working Example

See `StorageSettingsModule.cs` in the SmarterHauling mod for a full implementation that:
- Shows a custom chip displaying whitelist categories
- Adds a settings button
- Opens a custom window on click
- Uses sprites from the game
- Matches vanilla styling

Registration (in `SmarterHaulingPlugin.cs`):
```csharp
private void RegisterMegaTooltipModules()
{
    LaunchControl.RegisterItemModule<StorageSettingsModule>(StorageSettingsModule.SetupUI)
        .OnlyShowIf(co => co?.GetContainer() != null)
        .InsertAfter("ValueModule");
}
```

## Troubleshooting

### Module Not Appearing

1. Check that your visibility predicate returns true
2. Verify `SetData` doesn't set `_IsMarkedForDestroy = true`
3. Ensure your module is registered before any tooltips are shown
4. Check logs for registration messages

### UI Elements Not Found in SetData

If components are null:
- Ensure `SetupUI` creates all necessary GameObjects
- Use `Transform.Find()` to locate child objects by name
- Check that names match between `SetupUI` and `SetData`

### Module Appears in Wrong Position

- Check that target module name is spelled correctly
- Target module must exist in the vanilla prefab list
- Use `InsertBefore("ToggleMoreModule")` to appear before "Show More"

### Template Creation Errors

- Ensure `SetupUI` is static
- Don't access instance fields in `SetupUI`
- Add all components to the `go` parameter, not `gameObject`

### Click Handlers Not Working

- Add click listeners in `SetData`, not `SetupUI`
- Use lambda captures carefully with instance fields
- Ensure button component exists before adding listener

## Performance Considerations

- Templates are created once and cached
- GameObjects are cloned, not recreated from scratch
- Visibility predicates run on every tooltip show - keep them fast
- Avoid expensive operations in `SetData`
- Use `OnUpdateUI` sparingly

## Advanced Topics

### Accessing Reflection Fields

If you need to access private fields from CondOwner or other game classes:

```csharp
using System.Reflection;

FieldInfo field = typeof(CondOwner).GetField("_privateField", 
    BindingFlags.NonPublic | BindingFlags.Instance);
object value = field?.GetValue(co);
```

Cache `FieldInfo` instances to avoid repeated reflection calls.

### Dynamic Positioning

You can't change positioning after registration, but you can use multiple registrations with different predicates to conditionally insert at different positions.

### Module Communication

Modules can communicate through static fields or events:

```csharp
public static class ModuleSharedState
{
    public static event Action<string> OnDataChanged;
    public static string SharedData { get; set; }
}
```

## See Also

- [Command System](Command-System.md) - Register console commands
- [Sprite Utility](Sprite-Utility.md) - Find and use game sprites
- [DumpUI Command](DumpUI-Command.md) - Inspect game UI hierarchy

