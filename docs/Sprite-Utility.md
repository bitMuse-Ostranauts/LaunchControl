# Sprite Utility

The Sprite Utility makes it easy to load sprites from the game without writing custom search code. Search for sprites by keywords, and SpriteUtility finds them for you.

## Why Use This?

When you're creating custom UI, you probably want it to match the game's style. That means using the game's sprites. Without SpriteUtility, you'd need to:
1. Search through all loaded sprites manually
2. Match sprite names with string operations
3. Handle caching yourself
4. Write the same code in every mod

SpriteUtility does all that for you in one line.

## Basic Usage

```csharp
using Ostranauts.Bit;
using UnityEngine.UI;

// Load a sprite by searching for keywords
Sprite mySprite = SpriteUtility.FindSpriteByKeywords(
    new[] { "gradient", "bordered" } // Keywords to match (all must be present)
);

// Use it in your UI
Image myImage = gameObject.AddComponent<Image>();
myImage.sprite = mySprite;
myImage.type = Image.Type.Sliced;
```

## How It Works

SpriteUtility:
1. Searches all loaded sprites in memory using `Resources.FindObjectsOfTypeAll<Sprite>()`
2. Checks if sprite names match all your keywords (case-insensitive)
3. Caches the result for faster subsequent lookups
4. Returns the sprite (or null if not found)

## Finding Sprites

### Signature
```csharp
Sprite FindSpriteByKeywords(string[] keywords, bool useCache = true)
```

### Parameters

**`keywords`** (required)
Array of keywords that must appear in the sprite name. Case-insensitive, and ALL keywords must match.

Example: `new[] { "gradient", "bordered" }`, `new[] { "crewbar" }`, `new[] { "arrow" }`

**`useCache`** (optional, default: true)
Whether to cache the result. Usually you want this enabled for performance.

### Returns
- The `Sprite` if found
- `null` if not found

### Discovering Available Sprites

Use the `listsprites` command to discover what sprites are available:

```
# List all sprites (first 50)
listsprites

# Filter by keyword
listsprites gradient

# Get more results
listsprites gradient 100
```

This is essential for finding the right keywords to use!

## Examples

### Example 1: Simple Sprite Loading

```csharp
// Find a button sprite
Sprite buttonSprite = SpriteUtility.FindSpriteByKeywords(new[] { "circbutton" });

if (buttonSprite != null)
{
    myButton.image.sprite = buttonSprite;
}
else
{
    Debug.LogWarning("Button sprite not found!");
}
```

### Example 2: Multiple Keywords

```csharp
// Find a sprite that has both "gradient" and "bordered" in its name
Sprite chipBg = SpriteUtility.FindSpriteByKeywords(new[] { "gradient", "bordered" });
```

### Example 3: Single Keyword

```csharp
// Find a sprite with a single keyword
Sprite arrow = SpriteUtility.FindSpriteByKeywords(new[] { "arrow" });
```

### Example 4: Creating Matching UI

```csharp
using Ostranauts.Bit;
using UnityEngine;
using UnityEngine.UI;

public class MyCustomUI : MonoBehaviour
{
    private void CreateUI()
    {
        // Load sprites that match the game's style
        Sprite bgSprite = SpriteUtility.FindSpriteByKeywords(new[] { "gradient", "bordered" });
        Sprite buttonSprite = SpriteUtility.FindSpriteByKeywords(new[] { "circbutton" });
        
        // Create a panel
        GameObject panel = new GameObject("MyPanel");
        panel.transform.SetParent(transform);
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = bgSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.2f, 0.3f, 0.5f, 1f);
        
        // Create a button
        GameObject button = new GameObject("MyButton");
        button.transform.SetParent(panel.transform);
        
        Image buttonImage = button.AddComponent<Image>();
        buttonImage.sprite = buttonSprite;
        buttonImage.type = Image.Type.Sliced;
        
        Button buttonComponent = button.AddComponent<Button>();
        buttonComponent.onClick.AddListener(() => {
            Debug.Log("Button clicked!");
        });
    }
}
```

## Finding the Right Keywords

Use the built-in commands to discover sprite names:

### Method 1: listsprites Command (Easiest)

```
# List all sprites
listsprites

# Filter by keyword
listsprites gradient
listsprites button
listsprites sliced
```

### Method 2: DumpUI Command

Use [DumpUI Command](DumpUI-Command.md) to explore the game's UI:

1. Type `dumpui` in console
2. Click on any UI element
3. Check the logs for sprite names

Example output:
```
[Image] - Image [Sprite: Rounded Corner Rect POT 9-Sliced, Color: ...]
```

From this you know:
- Sprite name: `"Rounded Corner Rect POT 9-Sliced"`
- Keywords: `"rounded", "corner"` or `"sliced"`

## Caching

Sprites are automatically cached for performance. The cache key is based on the keywords used.

### Clear the Cache

```csharp
SpriteUtility.ClearCache();
```

Useful if you're dynamically loading/unloading content and want to free memory.

### Check Cache Stats

```csharp
string stats = SpriteUtility.GetCacheStats();
Debug.Log(stats); // "SpriteUtility Cache: 5 sprites, 0 prefabs"
```

## Tips & Tricks

### Tip 1: Be Specific with Keywords
More specific = faster and more reliable.

```csharp
// Less specific (might match multiple sprites)
Sprite s1 = SpriteUtility.FindSpriteByKeywords(new[] { "rounded" });

// More specific (better!)
Sprite s2 = SpriteUtility.FindSpriteByKeywords(new[] { "rounded", "corner", "9-sliced" });
```

### Tip 2: Use listsprites to Discover
Before writing code, use the command:

```
listsprites gradient
```

This shows you exactly what sprites match your keywords!

### Tip 3: Handle Nulls
Always check if the sprite was found!

```csharp
Sprite sprite = SpriteUtility.FindSpriteByKeywords(new[] { "keywords" });

if (sprite == null)
{
    Debug.LogWarning("Sprite not found - using fallback");
    sprite = CreateFallbackSprite();
}

myImage.sprite = sprite;
```

### Tip 4: Caching is Automatic
SpriteUtility caches results automatically, so calling the same keywords multiple times is fast.

## Other Utility Functions

### FindChildByPath

Helper to navigate GameObject hierarchies:

```csharp
GameObject child = SpriteUtility.FindChildByPath(parentObject, "Child/GrandChild/Target");
```

Supports `/` separated paths like file paths.

### GetAllSpritesInHierarchy

Get all sprites under a GameObject:

```csharp
List<Sprite> sprites = SpriteUtility.GetAllSpritesInHierarchy(
    myGameObject,
    "Background"  // Optional: start from this child
);

foreach (Sprite s in sprites)
{
    Debug.Log($"Found sprite: {s.name}");
}
```

Useful for discovering what sprites are available.

### FindSpriteByName

Find a sprite by exact name (searches all loaded sprites):

```csharp
Sprite sprite = SpriteUtility.FindSpriteByName("GUIArrow");
```

Less reliable than `FindSprite` because it searches globally, but can be useful in a pinch.

## Troubleshooting

**Sprite not found?**
1. Use `listsprites <keyword>` to see what sprites are available
2. Use `dumpui` to verify sprite names in the game UI
3. Check that all keywords match (they're case-insensitive but must all be present)
4. Try simpler keywords first

**Wrong sprite returned?**
Make your keywords more specific. The first match is returned, so if multiple sprites match, you might not get the one you want.

**Performance issues?**
- Make sure caching is enabled (`useCache: true` - it's the default)
- Load sprites during initialization, not every frame
- Use `SpriteUtility.GetCacheStats()` to check cache usage

**Null reference exceptions?**
Always check if the sprite is null before using it!

```csharp
Sprite sprite = SpriteUtility.FindSpriteByKeywords(new[] { "keywords" });
if (sprite != null)
{
    // Safe to use
}
```

## Real-World Example

Here's how SmarterHauling uses SpriteUtility:

```csharp
private void CreateUI()
{
    // Load sprites by keywords
    Sprite chipSprite = SpriteUtility.FindSpriteByKeywords(new[] { "gradient", "bordered" });
    Sprite buttonSprite = SpriteUtility.FindSpriteByKeywords(new[] { "circbutton" });
    
    // Handle missing sprites
    if (chipSprite == null)
    {
        Logger.LogWarning("Failed to load chip sprite");
        return;
    }
    
    // Create chip background
    _chipBackground = _chipContainer.AddComponent<Image>();
    _chipBackground.sprite = chipSprite;
    _chipBackground.type = Image.Type.Sliced;
    _chipBackground.color = new Color(0.5f, 0.7f, 1f, 1f);
    
    // Create settings button
    Image buttonImage = buttonObj.AddComponent<Image>();
    buttonImage.sprite = buttonSprite;
    buttonImage.type = Image.Type.Sliced;
}
```

Clean, simple, and it perfectly matches the game's UI style!

---

[← Command System](Command-System.md) | [Back to Home](Home.md) | [DumpUI Command →](DumpUI-Command.md)

