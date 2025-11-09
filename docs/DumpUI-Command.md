# DumpUI Command

The `dumpui` command is a built-in debugging tool that lets you explore Ostranauts' UI hierarchy. It's incredibly useful for figuring out how the game's UI is structured and finding sprites for your mods.

## What Does It Do?

When you run `dumpui`, it waits for you to click on any UI element. When you click, it dumps the entire UI hierarchy to the log, showing:
- GameObject names and hierarchy
- Components on each GameObject
- Sprite names
- RectTransform properties
- Text content
- Prefab information (when available)

Think of it as "Inspect Element" for Ostranauts.

## How to Use

1. **Open the console** (`` ` `` key)
2. **Type `dumpui`** and press Enter
3. **Click on any UI element** you want to inspect
4. **Check the logs** (BepInEx console or log file)

That's it!

## Example Output

Here's what you might see when you click on a crew status bar:

```
[Info: LaunchControl] DumpUI: Root Canvas: Canvas Crew Bar
[Info: LaunchControl] ==================== UI Hierarchy Dump ====================
[Info: LaunchControl] [Canvas Crew Bar] (Layer: UI, active)
[Info: LaunchControl]   - RectTransform [AnchoredPos: (0.0, 0.0), Size: (1152.0, 720.0)]
[Info: LaunchControl]   - Canvas [RenderMode: ScreenSpaceCamera, SortOrder: 60]
[Info: LaunchControl]   [GUICrewStatus] (Layer: UI, active)
[Info: LaunchControl]     - RectTransform [AnchoredPos: (0.0, 0.0), Size: (0.0, 0.0)]
[Info: LaunchControl]     - Image [Sprite: null, Color: RGBA(0.000, 0.000, 0.000, 1.000)]
[Info: LaunchControl]     [bgBorder] (Layer: UI, active)
[Info: LaunchControl]       - RectTransform [AnchoredPos: (0.0, 0.0), Size: (2.0, 2.0)]
[Info: LaunchControl]       - Image [Sprite: BorderSliced3px, Color: RGBA(1.000, 1.000, 1.000, 1.000)]
[Info: LaunchControl] ==================== End of Dump ====================
```

## Reading the Output

### Hierarchy Structure
Indentation shows parent-child relationships:
```
[Parent]
  - Component
  [Child]
    - Component
    [GrandChild]
```

### GameObject Info
```
[GameObjectName] (Layer: LayerName, active/inactive, Prefab: PrefabName)
```

- **Name**: The GameObject's name
- **Layer**: Unity layer (useful for raycasting)
- **Status**: Whether it's active
- **Prefab**: Original prefab name (when it's a clone)

### Component Details

Different components show different info:

**RectTransform:**
```
- RectTransform [AnchoredPos: (x, y), Size: (width, height)]
```

**Image:**
```
- Image [Sprite: SpriteName, Color: RGBA(r, g, b, a)]
```

**Text/TextMeshProUGUI:**
```
- TextMeshProUGUI [Text: "Some text content..."]
```

**Button:**
```
- Button [Interactable: True]
```

**Canvas:**
```
- Canvas [RenderMode: ScreenSpaceCamera, SortOrder: 60]
```

## Common Use Cases

### Use Case 1: Finding Sprites

You want to use a specific sprite from the game:

1. Run `dumpui`
2. Click on a UI element that uses the sprite you want
3. Look for the `Image` component in the output
4. Note the **Sprite name** and **Prefab name**

Example:
```
[ValueModule(Clone)] (Prefab: ValueModule)
  [Image]
    - Image [Sprite: Rounded Corner Rect POT 9-Sliced x32 _ gradient bordered, ...]
```

Now you know:
- Sprite name: `"Rounded Corner Rect POT 9-Sliced x32 _ gradient bordered"`
- Keywords: `"gradient", "bordered"` or `"sliced"`

Use it with SpriteUtility:
```csharp
Sprite sprite = SpriteUtility.FindSpriteByKeywords(new[] { "gradient", "bordered" });
```

Or use `listsprites gradient` to search for matching sprites.

### Use Case 2: Understanding UI Layout

You want to create similar UI:

1. Run `dumpui` on the reference UI
2. Look at the RectTransform values
3. Note the parent-child structure
4. Replicate it in your mod

Example output shows you:
- Sizes and positions
- Anchor points
- Layout groups
- Nested structure

### Use Case 3: Finding Prefab Names

You want to know what prefab a UI element comes from:

1. Run `dumpui`
2. Click on the element
3. Look for `(Prefab: PrefabName)` in the output

Elements instantiated from prefabs will show `(Clone)` in their name and list the original prefab.

### Use Case 4: Discovering Components

You want to know what components are on a GameObject:

1. Run `dumpui`
2. Click on the element
3. All components are listed after the GameObject name

This helps you understand:
- What makes buttons interactive
- How tooltips are attached
- Which layout groups are used
- Special game-specific components

## Tips & Tricks

### Tip 1: Start at the Root

The dump starts at the root Canvas by default, giving you the full context. This is usually what you want.

### Tip 2: Search the Logs

The output can be long! Use your log viewer's search function to find what you need:
- Search for sprite names
- Search for specific GameObjects
- Search for component types

### Tip 3: Compare Multiple Elements

Run `dumpui` on several similar elements to see the patterns:
- What do all buttons have in common?
- How do different tooltips differ?
- Which components are reused?

### Tip 4: Save the Output

If you're doing extensive UI work, save the dump to a file for reference:

1. Find the BepInEx log file
2. Run `dumpui` on all the elements you care about
3. Copy the relevant sections to a text file
4. Use it as a reference while coding

### Tip 5: Look for Patterns

UI elements often follow patterns:
```
Container
  Background (Image with 9-sliced sprite)
    Content (with layout group)
      Item1
      Item2
      Item3
```

Understanding these patterns helps you create consistent UI.

## Example Workflow

Let's say you want to create a button that matches the game's style:

**Step 1: Find a reference button**
- Look for any button in the game
- Run `dumpui` and click it

**Step 2: Note the details**
```
[Button] (Layer: UI, active)
  - RectTransform [AnchoredPos: (5.0, 0.0), Size: (-5.0, 0.0)]
  - Image [Sprite: GUICrewBarCircButtonMid, Color: RGBA(1.000, 1.000, 1.000, 1.000)]
  - Button [Interactable: True]
  [Text] (Layer: UI, active)
    - TextMeshProUGUI [Text: "Show More"]
```

**Step 3: Extract what you need**
- Button sprite: `GUICrewBarCircButtonMid`
- Button has Image + Button components
- Text is a child GameObject with TextMeshProUGUI
- White color, interactable

**Step 4: Replicate in your code**
```csharp
// Load the sprite (use listsprites to find available sprites)
Sprite buttonSprite = SpriteUtility.FindSpriteByKeywords(new[] { "circbutton" });

// Create button
GameObject buttonObj = new GameObject("MyButton");
buttonObj.transform.SetParent(parent);

// Add Image
Image img = buttonObj.AddComponent<Image>();
img.sprite = buttonSprite;
img.color = Color.white;
img.type = Image.Type.Sliced;

// Add Button component
Button btn = buttonObj.AddComponent<Button>();
btn.interactable = true;

// Add text child
GameObject textObj = new GameObject("Text");
textObj.transform.SetParent(buttonObj.transform);
TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
text.text = "My Button";
```

Perfect match! 🎯

## What Gets Dumped?

### Automatically Detected Components

DumpUI has special handling for these components:

- **RectTransform** - Position and size
- **Canvas** - Render mode and sort order
- **Image** - Sprite name and color
- **Text/TextMeshProUGUI** - Text content (truncated if long)
- **Button** - Interactable state
- **CanvasGroup** - Alpha, interactable, blocks raycasts

Other components just show their type name.

### Hierarchy Details

- Full parent-child structure
- GameObject names
- Layer information
- Active/inactive state
- Prefab origins (when available)

## Troubleshooting

**Nothing happens when I click?**
- Make sure you clicked on UI (not 3D objects)
- Check that the console received the command
- Look for error messages in the logs

**No UI elements found?**
- You might have clicked on empty space
- Try clicking directly on a visible UI element (button, text, panel)

**Output is too long?**
- That's normal for complex UI!
- Use search in your log viewer
- Or click on a simpler UI element

**Can't find the logs?**
BepInEx logs are usually at:
```
<Game Directory>/BepInEx/LogOutput.log
```

Or use a log viewer like BepInEx Log Viewer.

## Under the Hood

For the curious, here's what `dumpui` does:

1. Waits for mouse click
2. Uses Unity's EventSystem to raycast for UI
3. Finds the root Canvas
4. Recursively walks the hierarchy
5. Inspects each GameObject and its components
6. Logs everything in a readable format

It's non-destructive - it only reads data, never modifies anything.

## See Also

- [Sprite Utility](Sprite-Utility.md) - Use the sprites you find with `dumpui`
- [Command System](Command-System.md) - How commands like `dumpui` work

---

[← Sprite Utility](Sprite-Utility.md) | [Back to Home](Home.md)

