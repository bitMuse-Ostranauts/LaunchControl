# PersistentData System

## Overview

The PersistentData system is a centralized, thread-safe framework in LaunchControl that allows mods to save and load custom data with Ostranauts game saves. It integrates seamlessly with the game's save/load architecture and ensures your mod's data is automatically included in save ZIP files.

## Key Features

- **Automatic Integration**: Hooks into the game's save/load flow without requiring manual patches
- **Thread-Safe**: Safe handler registration even during concurrent mod loading
- **Error Isolation**: Exceptions in one mod's handler won't affect other mods or the game save
- **Simple API**: Just inherit from `PersistentDataHandler` and register your handler
- **ZIP Compatible**: Your mod's data is automatically included in compressed save files
- **Validation Support**: Control when your mod should save/load data

## Architecture

The PersistentData system consists of three main components:

### 1. PersistentDataHandler (Abstract Base Class)

Your mod creates a class that inherits from `PersistentDataHandler` and implements the save/load logic.

**Location**: `Mods/LaunchControl/PersistentData/PersistentDataHandler.cs`

### 2. PersistentDataManager (Singleton)

Manages all registered handlers and coordinates save/load operations.

**Location**: `Mods/LaunchControl/PersistentData/PersistentDataManager.cs`

### 3. PersistentDataPatches (Harmony Patches)

Hooks into Ostranauts' save/load flow to trigger handler callbacks at the right times.

**Location**: `Mods/LaunchControl/Patches/PersistentDataPatches.cs`

## Save/Load Flow

### Save Flow

```
User triggers save (F5 or menu)
    ↓
LoadManager.SaveGame() starts
    ↓
Game creates save folder and writes main game data
    ↓
[PATCH] PersistentDataPatches.SaveGame_Postfix()
    ↓
PersistentDataManager.SaveAll(saveFolderPath)
    ↓
For each registered handler:
    - Check handler.CanSave()
    - If true: Create mods/{moduleName}/ folder
    - Call handler.Save(modFolderPath)
    - Log success or error
    ↓
SavingJob compresses everything (including mods/) into ZIP
    ↓
Save complete
```

### Load Flow

```
User loads save from menu
    ↓
LoadManager extracts ZIP (to memory or temp folder)
    ↓
CrewSim.LoadGame() loads main game save
    ↓
CrewSim.OnGameFinishedLoading event fires
    ↓
[LISTENER] PersistentDataPatches.OnGameFinishedLoading()
    ↓
PersistentDataManager.LoadAll(saveFolderPath)
    ↓
For each registered handler:
    - Check if mods/{moduleName}/ folder exists
    - Check handler.CanLoad()
    - If true: Call handler.Load(modFolderPath)
    - Log success or error
    ↓
Load complete
```

## How to Use

There are two approaches to implementing save/load for your mod:

1. **Easy Way**: Use `ListPersistentDataHandler<T>` for simple list-based data (recommended for most cases)
2. **Custom Way**: Inherit directly from `PersistentDataHandler` for complex scenarios

### Approach 1: Using ListPersistentDataHandler<T> (Recommended)

For most mods that need to save a list of objects, use the generic `ListPersistentDataHandler<T>`. This handles all the JSON serialization automatically.

#### Step 1A: Create Your Data Handler

```csharp
using Ostranauts.Bit.PersistentData;
using System.Collections.Generic;
using System.Linq;

namespace YourMod.Data
{
    /// <summary>
    /// Example: Save a list of player-configured settings
    /// </summary>
    public class YourModDataHandler : ListPersistentDataHandler<YourDataClass>
    {
        // REQUIRED: Module name (lowercase, alphanumeric)
        public override string ModuleName => "yourmod";
        
        // REQUIRED: Validation methods
        public override bool CanSave() => true;
        public override bool CanLoad() => true;
        
        // OPTIONAL: Custom filename (default is "data.json")
        protected override string FileName => "mydata.json";
        
        // REQUIRED: Get the list to save
        protected override List<YourDataClass> GetDataList()
        {
            // Return your mod's data as a list
            return YourModSystem.GetAllData();
        }
        
        // REQUIRED: Set the loaded list
        protected override void SetDataList(List<YourDataClass> dataList)
        {
            // Apply the loaded data to your mod's systems
            YourModSystem.LoadData(dataList);
        }
        
        // OPTIONAL: Clear data before loading
        protected override void ClearData()
        {
            YourModSystem.ClearAllData();
        }
        
        // OPTIONAL: Validate items before saving
        protected override bool ValidateItem(YourDataClass item)
        {
            return item != null && item.IsValid();
        }
        
        // OPTIONAL: Process items after loading
        protected override YourDataClass ProcessLoadedItem(YourDataClass item)
        {
            // You can validate, migrate, or transform loaded items
            return item;
        }
    }
    
    // Your data class - LitJson serializes properties and fields automatically
    public class YourDataClass
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Id);
        }
    }
}
```

That's it! The generic handler automatically:
- Serializes your list to JSON using **LitJson** (the same library the game uses)
- Saves to `{savepath}/mods/yourmod/mydata.json`
- Loads and deserializes from JSON
- Handles missing files gracefully
- Validates items using your `ValidateItem()` method

**Important**: Your data class must be serializable by **LitJson**:
- **Properties with getters/setters work great** (LitJson supports them!)
- Public fields also work
- Supported types: primitives, strings, enums, arrays, lists, dictionaries, nested objects
- NOT supported: circular references, Unity-specific types (Vector3, etc.)

LitJson is much more flexible than Unity's JsonUtility:
- ✅ Dictionaries serialize automatically
- ✅ Properties (get/set) work
- ✅ More forgiving with types
- ✅ Same library the game uses for all its data

### Approach 2: Custom PersistentDataHandler

For complex scenarios (custom file formats, multiple files, binary data, etc.), inherit directly from `PersistentDataHandler`.

#### Step 1B: Create Your Data Handler

Create a class that inherits from `PersistentDataHandler`:

```csharp
using Ostranauts.Bit.PersistentData;
using System;
using System.IO;

namespace YourMod.Data
{
    public class YourModDataHandler : PersistentDataHandler
    {
        // REQUIRED: Unique module name (lowercase, alphanumeric, no spaces)
        // This becomes the folder name: {savepath}/mods/{ModuleName}/
        public override string ModuleName => "yourmod";

        // REQUIRED: Validate before saving
        // Return false to skip saving this time
        public override bool CanSave()
        {
            return true; // Or check if you have data to save
        }

        // REQUIRED: Validate before loading
        // Return false to skip loading this time
        public override bool CanLoad()
        {
            return true;
        }

        // REQUIRED: Save your mod's data
        // modFolderPath is already created for you: {savepath}/mods/yourmod/
        public override void Save(string modFolderPath)
        {
            try
            {
                // Example: Save some JSON data
                string filePath = Path.Combine(modFolderPath, "mydata.json");
                string jsonData = GetMyModDataAsJson();
                File.WriteAllText(filePath, jsonData);
                
                LogInfo($"Saved data to {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to save: {ex.Message}");
                throw; // Re-throw so the system logs the error
            }
        }

        // REQUIRED: Load your mod's data
        // modFolderPath: {savepath}/mods/yourmod/
        public override void Load(string modFolderPath)
        {
            try
            {
                string filePath = Path.Combine(modFolderPath, "mydata.json");
                
                // Handle missing files gracefully (older saves won't have your data)
                if (!File.Exists(filePath))
                {
                    LogInfo("No save data found (this is normal for older saves)");
                    return;
                }
                
                string jsonData = File.ReadAllText(filePath);
                LoadMyModDataFromJson(jsonData);
                
                LogInfo($"Loaded data from {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load: {ex.Message}");
                throw;
            }
        }
        
        private string GetMyModDataAsJson() { /* your code */ }
        private void LoadMyModDataFromJson(string json) { /* your code */ }
    }
}
```

### Step 2: Register Your Handler

In your mod's plugin `Awake()` method, register your handler with LaunchControl:

```csharp
using BepInEx;
using Ostranauts.Bit;

[BepInPlugin("com.yourname.yourmod", "Your Mod", "1.0.0")]
[BepInDependency("com.ostranauts.LaunchControl", BepInDependency.DependencyFlags.HardDependency)]
public class YourModPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo("Your Mod is loading...");
        
        // Create and register your data handler
        var dataHandler = new YourModDataHandler
        {
            Logger = Logger  // Set your logger for handler logging
        };
        
        LaunchControl.Instance.PersistentData.RegisterHandler("yourmod", dataHandler);
        
        Logger.LogInfo("Registered persistent data handler");
    }
}
```

### Step 3: Done!

Your mod's data will now automatically save/load with game saves. The system handles:
- Creating the `mods/yourmod/` folder in saves
- Calling your Save() method when the game saves
- Calling your Load() method when the game loads
- Including your data in the save ZIP
- Error handling and logging

## API Reference

### ListPersistentDataHandler\<T\> (Generic Class)

A generic implementation for saving/loading lists of objects. Most mods should use this.

**Inherits from**: `PersistentDataHandler`

#### Properties

- **`string FileName { get; }`** (virtual, protected)
  - Default: `"data.json"`
  - Override to customize the filename

#### Abstract Methods (You Must Implement)

- **`List<T> GetDataList()`** (protected abstract)
  - Returns the list of objects to save
  - Return `null` or empty list if nothing to save

- **`void SetDataList(List<T> dataList)`** (protected abstract)
  - Receives the loaded list of objects
  - Apply the data to your mod's systems

#### Optional Override Methods

- **`void ClearData()`** (protected virtual)
  - Clear existing data before loading
  - Default: does nothing

- **`bool ValidateItem(T item)`** (protected virtual)
  - Validate an item before saving
  - Return `false` to skip invalid items
  - Default: returns `true` if item is not null

- **`T ProcessLoadedItem(T item)`** (protected virtual)
  - Process an item after loading
  - Return `null` to skip the item
  - Default: returns the item unchanged

- **`string SerializeList(List<T> list)`** (protected virtual)
  - Custom serialization logic
  - Default: uses LitJson with pretty-printing

- **`List<T> DeserializeList(string json)`** (protected virtual)
  - Custom deserialization logic
  - Default: uses LitJson

#### Sealed Methods (Don't Override)

- **`void Save(string modFolderPath)`** (sealed override)
  - Handled by the generic implementation

- **`void Load(string modFolderPath)`** (sealed override)
  - Handled by the generic implementation

### PersistentDataHandler (Abstract Class)

#### Properties

- **`string ModuleName { get; }`** (abstract)
  - Unique identifier for your mod
  - Used as folder name: `{savepath}/mods/{ModuleName}/`
  - Must be lowercase, alphanumeric, no spaces

- **`ManualLogSource Logger { get; set; }`**
  - Your mod's BepInEx logger
  - Set this when registering the handler

#### Methods

- **`bool CanSave()`** (abstract)
  - Called before Save() to validate if saving is needed
  - Return `false` to skip saving this time
  - Return `true` to proceed with saving

- **`bool CanLoad()`** (abstract)
  - Called before Load() to validate if loading is possible
  - Return `false` to skip loading this time
  - Return `true` to proceed with loading

- **`void Save(string modFolderPath)`** (abstract)
  - Called during game save to write your mod's data
  - `modFolderPath`: Full path to your mod's save folder (already created)
  - Write your files here: `Path.Combine(modFolderPath, "yourfile.json")`
  - Throw exceptions on error (they'll be caught and logged)

- **`void Load(string modFolderPath)`** (abstract)
  - Called after game load to read your mod's data
  - `modFolderPath`: Full path to your mod's save folder
  - Handle missing files gracefully (older saves won't have your data)
  - Throw exceptions on error (they'll be caught and logged)

- **`void LogInfo(string message)`** (protected helper)
  - Log info messages with mod name prefix
  - Example: `[yourmod] Saved 5 items`

- **`void LogWarning(string message)`** (protected helper)
  - Log warning messages with mod name prefix

- **`void LogError(string message)`** (protected helper)
  - Log error messages with mod name prefix

### PersistentDataManager

Access via `LaunchControl.Instance.PersistentData`

#### Methods

- **`void RegisterHandler(string moduleName, PersistentDataHandler handler)`**
  - Register a data handler for your mod
  - `moduleName`: Must match `handler.ModuleName`
  - Call during your mod's `Awake()` method
  - Thread-safe

- **`void UnregisterHandler(string moduleName)`**
  - Unregister a data handler
  - Rarely needed (handlers persist for game lifetime)
  - Thread-safe

- **`int GetHandlerCount()`**
  - Get the number of registered handlers
  - Useful for debugging

## Complete Example: SmarterHauling

Here's a real-world example from the SmarterHauling mod that saves container whitelist preferences using the generic list handler:

### Handler Implementation

```csharp
using Ostranauts.Bit.PersistentData;
using Ostranauts.Bit.SmarterHauling.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Bit.SmarterHauling.Data
{
    /// <summary>
    /// Saves/loads container storage preferences using the generic list handler.
    /// Demonstrates how simple save/load can be with ListPersistentDataHandler.
    /// </summary>
    public class SmarterHaulingDataHandler : ListPersistentDataHandler<ContainerStoragePrefs>
    {
        public override string ModuleName => "smarterhauling";

        protected override string FileName => "container_whitelists.json";

        public override bool CanSave() => true;

        public override bool CanLoad() => true;

        /// <summary>
        /// Get the list of container storage preferences to save.
        /// Converts the dictionary to a list for serialization.
        /// </summary>
        protected override List<ContainerStoragePrefs> GetDataList()
        {
            var prefsDict = ContainerExtensions.GetAllWhitelists();
            if (prefsDict == null || prefsDict.Count == 0)
            {
                return null;
            }
            
            return prefsDict.Values.ToList();
        }

        /// <summary>
        /// Load the container storage preferences from the list.
        /// Converts the list back to a dictionary for the extension system.
        /// </summary>
        protected override void SetDataList(List<ContainerStoragePrefs> dataList)
        {
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            // Convert list to dictionary keyed by ContainerId
            var prefsDict = new Dictionary<string, ContainerStoragePrefs>();
            foreach (var prefs in dataList)
            {
                if (!string.IsNullOrEmpty(prefs.ContainerId))
                {
                    prefsDict[prefs.ContainerId] = prefs;
                }
            }

            // Load into the extension system
            ContainerExtensions.LoadWhitelists(prefsDict);
        }

        /// <summary>
        /// Clear existing whitelists before loading.
        /// </summary>
        protected override void ClearData()
        {
            ContainerExtensions.ClearAllWhitelists();
        }

        /// <summary>
        /// Validate that a container preference has a valid ContainerId.
        /// </summary>
        protected override bool ValidateItem(ContainerStoragePrefs item)
        {
            return item != null && !string.IsNullOrEmpty(item.ContainerId);
        }
    }
}
```

### Plugin Registration

```csharp
[BepInPlugin("com.ostranauts.smarterhauling", "Smarter Hauling", "2.0.0")]
[BepInDependency("com.ostranauts.LaunchControl", BepInDependency.DependencyFlags.HardDependency)]
public class SmarterHaulingPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo("SmarterHauling is loading...");
        
        // Initialize other systems...
        
        // Register persistent data handler
        RegisterPersistentDataHandler();
        
        // Apply patches...
    }
    
    private void RegisterPersistentDataHandler()
    {
        try
        {
            var handler = new SmarterHaulingDataHandler
            {
                Logger = Logger
            };

            LaunchControl.Instance.PersistentData.RegisterHandler("smarterhauling", handler);
            
            Logger.LogInfo("Registered persistent data handler for container whitelists");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error registering persistent data handler: {ex.Message}");
        }
    }
}
```

## Error Handling

The PersistentData system is designed to be resilient:

### Handler Exceptions

- Exceptions thrown in `Save()` or `Load()` are caught and logged
- The error won't crash the game or abort other handlers
- Your handler's logger will receive the error message
- LaunchControl's logger will log the full stack trace

### Validation

Use `CanSave()` and `CanLoad()` to:
- Check if you have data worth saving
- Validate system state before operations
- Gracefully skip operations when not needed

Example:
```csharp
public override bool CanSave()
{
    // Don't save if we have no data
    return GetMyDataCount() > 0;
}

public override bool CanLoad()
{
    // Don't load if our system isn't initialized
    return IsMySystemInitialized();
}
```

### Missing Data

Always handle missing files in `Load()`:

```csharp
public override void Load(string modFolderPath)
{
    string filePath = Path.Combine(modFolderPath, "mydata.json");
    
    // Older saves won't have your mod's data
    if (!File.Exists(filePath))
    {
        LogInfo("No save data found (normal for older saves)");
        return;
    }
    
    // Load your data...
}
```

## Thread Safety

### Main Thread Execution

Your `Save()` and `Load()` methods run on the main Unity thread, NOT on background threads. This means:

✅ You can safely:
- Access Unity objects and GameObjects
- Call Unity APIs
- Modify game state
- Access static collections

❌ Don't:
- Block for long periods (keep saves/loads fast)
- Wait on background threads
- Use Unity's `Threading` APIs unnecessarily

### Handler Registration

`RegisterHandler()` should be called from your mod's `Awake()` method on the main Unity thread. Since BepInEx loads mods sequentially during startup, there's no need for complex thread synchronization.

## Best Practices

### 1. Keep Saves Fast

Your `Save()` method runs during the game save process. Keep it fast:

```csharp
// ✅ Good - Fast serialization
public override void Save(string modFolderPath)
{
    string json = JsonUtility.ToJson(myData);
    File.WriteAllText(Path.Combine(modFolderPath, "data.json"), json);
}

// ❌ Bad - Slow operations
public override void Save(string modFolderPath)
{
    // Don't do expensive computations during save
    for (int i = 0; i < 1000000; i++)
    {
        ComplexCalculation();
    }
}
```

### 2. Version Your Save Data

Include a version number in your save format:

```csharp
[Serializable]
public class MySaveData
{
    public int Version = 1;
    public List<string> Items;
}
```

Then handle version migrations in `Load()`:

```csharp
public override void Load(string modFolderPath)
{
    var data = LoadDataFromFile();
    
    if (data.Version == 1)
    {
        // Migrate to version 2
        data = MigrateToV2(data);
    }
    
    ApplyData(data);
}
```

### 3. Use Descriptive File Names

Use clear, descriptive file names:

```csharp
// ✅ Good
"container_whitelists.json"
"ship_modifications.json"
"player_preferences.json"

// ❌ Bad
"data.json"
"save.dat"
"temp.txt"
```

### 4. Handle Corrupted Data

Always validate loaded data:

```csharp
public override void Load(string modFolderPath)
{
    try
    {
        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<MySaveData>(json);
        
        // Validate the data
        if (data == null || !data.IsValid())
        {
            LogWarning("Save data is corrupted, using defaults");
            data = CreateDefaultData();
        }
        
        ApplyData(data);
    }
    catch (Exception ex)
    {
        LogError($"Failed to load, using defaults: {ex.Message}");
        ApplyData(CreateDefaultData());
    }
}
```

### 5. Log Appropriately

Use the helper logging methods:

```csharp
LogInfo("Normal operation messages");      // Regular status
LogWarning("Non-critical issues");         // Problems that don't break functionality
LogError("Critical failures");             // Serious errors
```

## Troubleshooting

### My handler isn't being called

**Check:**
1. Did you register the handler in `Awake()`?
2. Is your BepInEx dependency correct: `[BepInDependency("com.ostranauts.LaunchControl")]`
3. Is LaunchControl loading before your mod?
4. Check BepInEx logs for registration messages

### My save data isn't in the ZIP

**Check:**
1. Does your `Save()` method actually write files?
2. Check for exceptions in BepInEx logs
3. Look in the uncompressed save folder before ZIP compression
4. Verify `CanSave()` returns `true`

### Load() isn't finding my files

**Check:**
1. Is the `mods/{modulename}/` folder in the save?
2. Are you using `Path.Combine()` correctly?
3. Check the exact spelling of your file names
4. Look at the save folder path in logs

### Duplicate handlers error

**Problem:** You're registering the same module name twice

**Solution:** Only call `RegisterHandler()` once per mod, in `Awake()`

### Performance issues

**Problem:** Save/load is slow

**Solutions:**
- Profile your `Save()`/`Load()` methods
- Reduce data size (only save what's necessary)
- Use efficient serialization
- Avoid complex calculations during save/load

## FAQ

### Q: Should I use ListPersistentDataHandler or PersistentDataHandler?

A: Use `ListPersistentDataHandler<T>` for:
- Saving a list/collection of objects
- Simple to moderate data structures
- When LitJson can handle your types (which is most types!)
- Most common scenarios (95% of mods)

Use `PersistentDataHandler` directly for:
- Multiple different files
- Binary data formats
- Complex custom serialization
- Non-list data structures (dictionaries that don't convert well to lists)

### Q: Can I have multiple handlers per mod?

A: Technically yes (use different module names), but it's recommended to use one handler per mod and manage multiple data files within that handler's Save/Load methods.

### Q: What if my mod isn't installed when loading a save?

A: The system gracefully handles this. The save will load normally, and the missing mod's data folder will simply be ignored.

### Q: What are the advantages of LitJson over Unity's JsonUtility?

A: LitJson (which the generic handler uses) is much more powerful:

**Properties**: LitJson serializes properties with get/set, Unity's JsonUtility only handles fields.
```csharp
// ✅ Works with LitJson
public string Name { get; set; }

// ❌ Doesn't work with JsonUtility (needs to be a field)
public string Name;
```

**Dictionaries**: LitJson handles dictionaries automatically, JsonUtility doesn't support them at all.
```csharp
// ✅ Works with LitJson
public Dictionary<string, int> Values { get; set; }

// ❌ Doesn't work with JsonUtility
```

**Flexibility**: LitJson is more forgiving with type conversions and nested structures.

**Consistency**: LitJson is what Ostranauts uses for ALL its game data, so your saves match the game's format.

### Q: Can I save binary data?

A: Yes! Use `File.WriteAllBytes()` and `File.ReadAllBytes()` in your Save/Load methods.

### Q: What happens on new game?

A: Your `Load()` method won't be called (there's no save to load). Initialize your mod's state as if starting fresh.

### Q: Can I access other mods' save data?

A: Technically yes (it's in `mods/{othermodname}/`), but this is not recommended. Mods should manage their own data independently.

### Q: How do I test my handler?

A: 
1. Register your handler
2. Set up your mod's data (e.g., change settings)
3. Save the game (F5)
4. Modify your mod's data
5. Load the save
6. Verify your data was restored

Check BepInEx logs for save/load messages from your handler.

## Support

For issues, questions, or contributions:
- Check BepInEx logs for detailed error messages
- Review this documentation
- Look at the SmarterHauling example implementation
- Report bugs with full BepInEx logs attached

## Technical Details

### File System Layout

```
{SavesPath}/{SaveName}/
├── {PlayerName}.json          (Main game save)
├── saveInfo.json              (Save metadata)
├── portrait.png               (Screenshots)
├── ships/
│   └── {ShipID}.json          (Ship data)
└── mods/                      ← Your data goes here
    ├── smarterhauling/
    │   └── container_whitelists.json
    ├── yourmod/
    │   ├── data.json
    │   └── settings.json
    └── anothermod/
        └── state.dat
```

After compression:
```
{SavesPath}/{SaveName}/
├── {SaveName}.zip             ← Contains everything above
├── portrait.png               (Kept uncompressed)
├── screenshot.png             (Kept uncompressed)
└── saveInfo.json              (Kept uncompressed)
```

### Timing Details

**Save Timing:**
- Triggered: User saves (F5, menu, autosave)
- Hook: `LoadManager.SaveGame()` Postfix
- When: After game data is prepared, before ZIP compression
- Thread: Main Unity thread (synchronous)

**Load Timing:**
- Triggered: User loads from menu
- Hook: `CrewSim.OnGameFinishedLoading` event
- When: After main game data is fully loaded
- Thread: Main Unity thread (synchronous)

### JSON Output Format

The `ListPersistentDataHandler<T>` uses **LitJson** (same as the game) to create clean, readable JSON arrays:

**Example output** (`container_whitelists.json`):
```json
[
  {
    "ContainerId": "container123",
    "AllowedCategories": [
      "IsFood",
      "IsDrink"
    ]
  },
  {
    "ContainerId": "container456",
    "AllowedCategories": [
      "IsTool",
      "IsWeapon"
    ]
  }
]
```

**Features:**
- Pretty-printed with 2-space indentation (matches game data files)
- Direct JSON array format (not wrapped in an object)
- Properties and fields both serialize
- Nested objects and collections work
- Easy to read, inspect, and manually edit if needed

**Why LitJson?**
- Same library Ostranauts uses for all game data
- More powerful than Unity's JsonUtility (properties, dictionaries, nested types)
- Consistent format with the rest of the game's files
- Better error messages and type handling

### Dependencies

Required dependencies in your mod:
- BepInEx (5.4.21+)
- LaunchControl (`[BepInDependency("com.ostranauts.LaunchControl")]`)

LaunchControl dependencies:
- Harmony (for patches)
- LitJson (included in Ostranauts Assembly-CSharp)
- Ostranauts Assembly-CSharp
- Unity Engine assemblies

