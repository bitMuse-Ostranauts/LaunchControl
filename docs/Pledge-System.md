# LaunchControl Pledge Registration System

## Overview

The LaunchControl Pledge System provides a simple, standardized way for mods to register custom AI behaviors (pledges) in Ostranauts. Instead of each mod creating its own Harmony patches and factory systems, they can use LaunchControl's centralized pledge registration API.

## Architecture

### Components

1. **`PledgeManager`**: Centralized manager for registering and creating custom pledge types
2. **`PledgeFactoryPatch`**: Harmony patches that inject custom pledges into the game's `PledgeFactory`
3. **`LaunchControl.RegisterPledgeType()`**: Public API for mods to register custom pledges

### How It Works

```
Mod registers pledge type
         ↓
LaunchControl.RegisterPledgeType()
         ↓
PledgeManager stores type mapping
         ↓
Game calls PledgeFactory.Factory()
         ↓
PledgeFactoryPatch intercepts
         ↓
Injects custom types into dictTypes
         ↓
Game creates pledge instances
         ↓
Custom pledge AI behavior runs
```

## Usage

### Basic Registration

To register a custom pledge type in your mod:

```csharp
// In your mod's plugin Awake() or initialization method
LaunchControl.RegisterPledgeType("yourpledgetype", typeof(YourCustomPledge));
```

### Complete Example

Here's a complete example from the SmarterHauling mod:

#### 1. Create Your Pledge Class

```csharp
using System;
using UnityEngine;

namespace YourMod.Pledges
{
    public class YourCustomPledge : Pledge2
    {
        public YourCustomPledge()
        {
            // Initialize any condition triggers or data
        }
        
        public override bool IsEmergency()
        {
            // Return true if this is an emergency pledge
            return false;
        }
        
        public override bool Do()
        {
            // Your pledge logic here
            if (base.Us == null || base.Us.HasCond("IsAIManual"))
            {
                return false;
            }
            
            if (this.Finished())
            {
                return true;
            }
            
            // Check conditions and queue interactions
            // ...
            
            return false;
        }
    }
}
```

#### 2. Register in Your Plugin

```csharp
using BepInEx;
using Ostranauts.Bit;

namespace YourMod
{
    [BepInPlugin("com.yourmod", "YourMod", "1.0.0")]
    [BepInDependency("com.ostranauts.LaunchControl", BepInDependency.DependencyFlags.HardDependency)]
    public class YourModPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // ... other initialization ...
            
            RegisterPledgeTypes();
        }
        
        private void RegisterPledgeTypes()
        {
            try
            {
                if (LaunchControl.Instance == null || LaunchControl.Instance.Pledges == null)
                {
                    Logger.LogError("LaunchControl pledge system not available");
                    return;
                }

                // Register your custom pledge type
                bool success = LaunchControl.RegisterPledgeType("yourcustompledge", typeof(Pledges.YourCustomPledge));
                
                if (success)
                {
                    Logger.LogInfo("Registered custom pledge types");
                }
                else
                {
                    Logger.LogError("Failed to register pledge type");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error registering pledge types: {ex.Message}");
            }
        }
    }
}
```

#### 3. Create JSON Data for Your Pledge

Create a JSON file in the game's `pledges/` folder (or via mod data loading):

```json
{
  "strName": "YourCustomPledge",
  "strType": "yourcustompledge",
  "strIATrigger": "",
  "strIAEmergency": "",
  "nPriority": 300,
  "bPlayerOnly": false,
  "bNPCOnly": false,
  "bCommandNegates": true,
  "bPlayerKnowledgeOnly": false,
  "bThemAllowDocked": true
}
```

#### 4. Add to Characters

Add the pledge to characters via console or in character definitions:

```
addpledge [CharacterID] YourCustomPledge
```

## API Reference

### LaunchControl.RegisterPledgeType()

```csharp
public static bool RegisterPledgeType(string pledgeTypeName, Type pledgeClass)
```

Registers a custom pledge type with the system.

**Parameters:**
- `pledgeTypeName` (string): The name used in JSON pledge definitions (`strType` field). Case-insensitive.
- `pledgeClass` (Type): The C# class type that implements the pledge (must inherit from `Pledge2`)

**Returns:**
- `bool`: `true` if registered successfully, `false` otherwise

**Requirements:**
- The pledge class must inherit from `Pledge2`
- The pledge class must have a parameterless constructor
- Must be called after LaunchControl is initialized (typically in your plugin's `Awake()` method)

**Example:**
```csharp
LaunchControl.RegisterPledgeType("swapbattery", typeof(PledgeSwapBattery));
```

### PledgeManager Instance

Access the pledge manager directly via `LaunchControl.Instance.Pledges`:

```csharp
// Check if a pledge type is registered
bool isRegistered = LaunchControl.Instance.Pledges.IsRegistered("swapbattery");

// Get count of registered pledge types
int count = LaunchControl.Instance.Pledges.RegisteredCount;

// Get all registered pledge type names
foreach (string typeName in LaunchControl.Instance.Pledges.GetRegisteredPledgeTypes())
{
    Debug.Log($"Registered pledge: {typeName}");
}
```

## Creating Custom Pledges

### Pledge2 Base Class

Your custom pledge must inherit from `Pledge2` and implement key methods:

```csharp
public class YourPledge : Pledge2
{
    // Required: Initialize condition triggers, etc.
    public YourPledge() { }
    
    // Required: Main pledge logic
    public override bool Do() { }
    
    // Optional: Define emergency conditions
    public override bool IsEmergency() { }
}
```

### Key Properties (Inherited)

- `Us` (CondOwner): The character executing the pledge
- `Them` (CondOwner): Target character/object (if any)
- `strName` (string): Pledge name from JSON
- `strIATrigger` (string): Interaction to trigger
- `strIAEmergency` (string): Emergency interaction name
- `nPriority` (int): Pledge priority (higher = more urgent)
- `bPlayerOnly` (bool): Only for player characters
- `bNPCOnly` (bool): Only for NPCs
- `bCommandNegates` (bool): Player commands cancel this pledge
- `bPlayerKnowledgeOnly` (bool): Only if player knows about it
- `bThemAllowDocked` (bool): Allow targets on docked ships

### Key Methods (Inherited)

```csharp
// Check if pledge is finished
protected bool Finished()

// Get priority level
public int Priority { get; }

// Check if this is an emergency
public virtual bool IsEmergency()
```

### Common Patterns

#### Finding Items

```csharp
// Get items matching a condition trigger
CondTrigger ctFood = DataHandler.GetCondTrigger("TIsFood");
List<CondOwner> foods = base.Us.ship.GetCOs(ctFood, true, false, false);
```

#### Pathfinding

```csharp
private bool IsReachable(CondOwner target)
{
    Pathfinder pathfinder = base.Us.Pathfinder;
    if (pathfinder == null || base.Us.ship == null)
        return false;
    
    Vector2 pos = target.GetPos("use", false);
    Tile tile = base.Us.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
    
    if (tile == null)
        return false;
    
    bool bAllowAirlocks = base.Us.HasAirlockPermission(false);
    PathResult result = pathfinder.CheckGoal(tile, 1f, target, bAllowAirlocks);
    
    return result.HasPath;
}
```

#### Queuing Interactions

```csharp
Interaction interaction = DataHandler.GetInteraction("ACTUse", null, false);
if (interaction != null)
{
    base.Us.QueueInteraction(target, interaction, true);
    return true;
}
```

#### Adding Cooldowns

```csharp
// Add a condition to prevent constant checking
base.Us.AddCondAmount("IsCooldownYourPledge", 1.0, 0.0, 0f);

// Later, check the cooldown
if (base.Us.HasCond("IsCooldownYourPledge"))
{
    return false;
}
```

## Pledge Priority Levels

Understanding priority helps you place your pledge appropriately:

| Priority Range | Type | Examples |
|----------------|------|----------|
| 1000+ | Critical Emergency | Suffocating, starving |
| 500-999 | High Emergency | Very hungry, dehydrated |
| 300-499 | Important | Maintenance, tool management |
| 100-299 | Normal | Work tasks, social |
| 1-99 | Low | Entertainment, exploration |

Choose a priority that makes sense for your pledge's urgency.

## JSON Pledge Definition

### Required Fields

```json
{
  "strName": "UniquePledgeName",
  "strType": "yourpledgetype",
  "nPriority": 300
}
```

### Optional Fields

```json
{
  "strIATrigger": "InteractionName",
  "strIAEmergency": "EmergencyInteractionName",
  "strThemID": "TargetID",
  "bThemAllowDocked": true,
  "bPlayerOnly": false,
  "bNPCOnly": false,
  "bCommandNegates": true,
  "bPlayerKnowledgeOnly": false
}
```

### Field Descriptions

- **strName**: Unique identifier for this pledge instance
- **strType**: Pledge type name (must match registered type)
- **strIATrigger**: Interaction to run when pledge triggers
- **strIAEmergency**: Interaction for emergency state
- **nPriority**: Higher numbers = higher priority
- **strThemID**: ID of target character/object
- **bThemAllowDocked**: Allow targeting docked ships
- **bPlayerOnly**: Only trigger for player characters
- **bNPCOnly**: Only trigger for NPCs
- **bCommandNegates**: Player commands override this
- **bPlayerKnowledgeOnly**: Only if player has knowledge

## Advanced Topics

### Multi-Phase Pledges

For complex behaviors, break them into phases:

```csharp
private int _currentPhase = 0;

public override bool Do()
{
    switch (_currentPhase)
    {
        case 0:
            // Phase 1: Find target
            if (FindTarget())
            {
                _currentPhase = 1;
            }
            break;
            
        case 1:
            // Phase 2: Move to target
            if (MoveToTarget())
            {
                _currentPhase = 2;
            }
            break;
            
        case 2:
            // Phase 3: Execute action
            if (ExecuteAction())
            {
                return true; // Finished
            }
            break;
    }
    
    return false;
}
```

### Conditional Emergency States

```csharp
public override bool IsEmergency()
{
    // Only emergency if condition is critical
    if (base.Us.GetCondAmount("YourStat") < 0.1)
    {
        return true;
    }
    
    return false;
}
```

### State Persistence

If your pledge needs to remember state across turns:

```csharp
private CondOwner _rememberedTarget;
private float _lastCheckTime;

public override bool Do()
{
    // Use remembered state
    if (_rememberedTarget != null && _rememberedTarget.IsValid())
    {
        // Continue with remembered target
    }
    else
    {
        // Find new target
        _rememberedTarget = FindTarget();
    }
}
```

## Debugging

### Enable Logging

Add debug logs to your pledge:

```csharp
Debug.Log($"[YourPledge] Character {base.Us.strNameFriendly} executing pledge");
```

### Console Commands

Useful commands for testing pledges:

```
# List all pledges for a character
listpledges [CharacterID]

# Add a pledge to a character
addpledge [CharacterID] YourPledgeName

# Remove a pledge from a character
removepledge [CharacterID] YourPledgeName

# Check character's current conditions
inspect [CharacterID]
```

### Common Issues

**Pledge Not Triggering:**
1. Check if registered: Look for registration message in logs
2. Verify JSON data is loaded
3. Ensure pledge is added to character
4. Check priority isn't too low
5. Verify conditions in `Do()` method

**Pledge Errors:**
1. Check inheritance from `Pledge2`
2. Ensure parameterless constructor exists
3. Verify `Us` is not null before using
4. Add try-catch blocks for safety

**Performance Issues:**
1. Add cooldowns to prevent constant checking
2. Cache expensive lookups
3. Early-exit from `Do()` when conditions aren't met
4. Avoid excessive pathfinding calls

## Best Practices

### Do's

✅ Always check `base.Us` is not null
✅ Use condition triggers for item lookups
✅ Add cooldowns for expensive operations
✅ Early-exit when conditions aren't met
✅ Use try-catch for error handling
✅ Test with multiple characters
✅ Document your pledge's behavior
✅ Choose appropriate priority levels

### Don'ts

❌ Don't perform expensive operations every frame
❌ Don't assume items/characters still exist
❌ Don't forget to check pathfinding
❌ Don't ignore emergency states
❌ Don't use hardcoded IDs
❌ Don't create infinite loops
❌ Don't modify game state in `IsEmergency()`

## Example: Complete Battery Swap Pledge

See `Mods/SmarterHauling/Pledges/PledgeSwapBattery.cs` for a complete, working example of a custom pledge that:
- Finds tools with low batteries
- Locates charging stations with good batteries
- Validates pathfinding and accessibility
- Queues appropriate interactions
- Uses cooldowns for performance
- Handles edge cases gracefully

## Migration Guide

If you have an existing pledge system using custom Harmony patches:

### Old Way (Per-Mod)

```csharp
[HarmonyPatch(typeof(PledgeFactory))]
public static class YourModPledgeFactory
{
    [HarmonyPrefix]
    public static void Factory_Prefix()
    {
        // Custom registration code...
    }
}
```

### New Way (Via LaunchControl)

```csharp
private void RegisterPledgeTypes()
{
    LaunchControl.RegisterPledgeType("yourtype", typeof(YourPledge));
}
```

**Benefits:**
- Less code to maintain
- No need for reflection
- Consistent across mods
- Better error handling
- Centralized logging

## Technical Details

### Injection Strategy

LaunchControl uses a two-phase injection strategy:

1. **Prefix Patch**: Injects custom types into `PledgeFactory.dictTypes` before the factory method runs
2. **Postfix Patch**: Fallback that creates custom pledges if the prefix didn't catch them

This dual approach ensures maximum compatibility even if the game's factory structure changes.

### Type Registration

All pledge types are stored in a case-insensitive dictionary:

```csharp
Dictionary<string, Type> _customPledgeTypes
```

When the game requests a pledge, LaunchControl looks up the type and creates an instance using reflection.

### Thread Safety

The current implementation is not explicitly thread-safe. Pledge registration should only be done during plugin initialization.

## Support and Troubleshooting

### Log Messages

Watch for these log messages:

```
[PledgeManager] Registered custom pledge type 'swapbattery' -> PledgeSwapBattery
[PledgeFactoryPatch] Successfully injected N custom pledge type(s) into PledgeFactory.dictTypes
[PledgeFactoryPatch] Created custom pledge 'YourPledge' of type 'yourtype' via postfix
```

### Error Messages

Common errors and solutions:

**"Class does not inherit from Pledge2"**
- Ensure your class extends `Pledge2`

**"Class does not have a parameterless constructor"**
- Add a `public YourPledge() { }` constructor

**"LaunchControl pledge system not available"**
- Check that LaunchControl is loaded as a dependency
- Verify `BepInDependency` attribute is set

## Version History

- **v1.0** (LaunchControl 2.0.0): Initial pledge registration system

## Credits

Created by: LaunchControl Team
Tested by: SmarterHauling mod
Game: Ostranauts by Blue Bottle Games

