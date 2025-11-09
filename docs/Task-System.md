# Task System

## Overview

The Task system is a centralized framework in LaunchControl that allows mods to store custom metadata on Task2 instances. This metadata persists across save/load cycles, enabling mods to track additional information about tasks beyond what the base game provides.

## Key Features

- **Persistent Storage**: Task metadata automatically saves and loads with game saves
- **Hash-Based Keys**: Stable keys generated from Task2 fields ensure data survives save/load
- **Simple API**: Easy-to-use methods for storing and retrieving task data
- **Automatic Cleanup**: Orphaned data is cleaned up before saves and when tasks are removed
- **Integration with Interactions**: Convenience methods to access task data from Interaction context
- **Type-Safe**: Generic methods provide type-safe storage and retrieval

## Architecture

The Task system consists of three main components:

### 1. TaskDataProvider (Singleton)

The core storage manager that holds task metadata in memory and provides the API for mods.

**Location**: `Mods/LaunchControl/Tasks/TaskDataProvider.cs`

**Key Method**: `GetTaskKey(Task2 task)` - Generates a stable hash from Task2 fields (strDuty, strInteraction, strTargetCOID, nTile, strTileShip)

### 2. TaskDataHandler (PersistentData Handler)

Handles serialization/deserialization of task data via the PersistentData system.

**Location**: `Mods/LaunchControl/Tasks/TaskDataHandler.cs`

**Saves to**: `mods/LaunchControl.tasks/task_data.json` within save folders

### 3. TaskPatches (Harmony Patches)

Ensures task data is cleaned up when tasks are removed from the game.

**Location**: `Mods/LaunchControl/Patches/TaskPatches.cs`

## Task Lifecycle

Understanding the Task2 lifecycle is important for using this system effectively:

```
Task Created by WorkManager
    ↓
Task claimed by NPC (becomes active)
    ↓
NPC executes Interactions (short-lived)
    ↓
Task completed or cancelled
    ↓
WorkManager.RemoveTask() called
    ↓
[PATCH] TaskPatches cleans up task data
```

### Important Notes

- **Task2** is the long-lived object representing a work task (e.g., "Haul this item")
- **Interaction** is the short-lived object representing individual steps (e.g., "PickupItemStack", "Walk", "DropItemStack")
- Only **unclaimed tasks** are saved by the base game (active tasks are not persisted)
- Task data persists as long as the Task2 exists

## Save/Load Flow

### Save Flow

```
User triggers save (F5 or menu)
    ↓
LoadManager.SaveGameData() starts
    ↓
[PATCH] PersistentDataPatches.SaveGameData_Prefix()
    ↓
TaskDataProvider.CleanupOrphanedData()
    - Removes data for tasks that no longer exist
    ↓
PersistentDataManager.SaveAll()
    ↓
TaskDataHandler.Save()
    - Serializes all task data to JSON
    - Writes to mods/LaunchControl.tasks/task_data.json
    ↓
Game compresses save folder to ZIP
    ↓
Save complete
```

### Load Flow

```
User loads save from menu
    ↓
LoadManager extracts ZIP or reads folder
    ↓
CrewSim.LoadGame() loads main game data
    ↓
WorkManager recreates Task2 instances from save data
    ↓
[PATCH] PersistentDataPatches.OnLoadSelectedSave()
    ↓
PersistentDataManager.LoadAll() or LoadAllFromMemory()
    ↓
TaskDataHandler.Load() or LoadFromMemory()
    - Deserializes task data from JSON
    - Restores data into TaskDataProvider
    ↓
Task data available for mods to query
    ↓
Load complete
```

## How to Use

### Direct Task Access

If you have a reference to a Task2 instance, you can directly store and retrieve data:

```csharp
// Store data on a task
Task2 haulTask = GetSomeTask();
LaunchControl.Instance.Tasks.SetData(haulTask, "MyMod_OriginalDestination", destinationTile);
LaunchControl.Instance.Tasks.SetData(haulTask, "MyMod_PickupCount", 3);

// Retrieve data from a task
int pickupCount = LaunchControl.Instance.Tasks.GetData<int>(haulTask, "MyMod_PickupCount");
int destinationTile = LaunchControl.Instance.Tasks.GetData<int>(haulTask, "MyMod_OriginalDestination");

// Check if data exists
if (LaunchControl.Instance.Tasks.HasData(haulTask, "MyMod_OriginalDestination"))
{
    // Task has our custom data
}

// Remove specific data
LaunchControl.Instance.Tasks.RemoveData(haulTask, "MyMod_PickupCount");

// Clear all data for a task
LaunchControl.Instance.Tasks.ClearTask(haulTask);
```

### Access from Interaction Context

When working with IEffect implementations or Interaction patches, use the convenience methods:

```csharp
public class MyCustomEffect : IEffect
{
    public bool ShouldExecute(Interaction interaction)
    {
        return interaction.strName == "PickupItemStack";
    }

    public void Execute(Interaction interaction)
    {
        // Get the Task2 associated with this interaction
        Task2 task = LaunchControl.Instance.Interactions.GetTask(interaction);
        
        if (task != null)
        {
            // Store data directly on the task
            LaunchControl.Instance.Tasks.SetData(task, "MyMod_FirstPickupTile", interaction.nTile);
        }

        // Or use the convenience method
        LaunchControl.Instance.Interactions.SetTaskData(interaction, "MyMod_FirstPickupTile", interaction.nTile);
        
        // Retrieve task data
        int firstPickupTile = LaunchControl.Instance.Interactions.GetTaskData<int>(interaction, "MyMod_FirstPickupTile");
        
        // Check if task has specific data
        if (LaunchControl.Instance.Interactions.HasTaskData(interaction, "MyMod_FirstPickupTile"))
        {
            // Task has our custom data
        }
    }
}
```

## API Reference

### TaskDataProvider

Access via `LaunchControl.Instance.Tasks`

#### SetData<T>(Task2 task, string key, T value)

Stores custom data on a Task2 instance.

**Parameters:**
- `task` - The Task2 instance to store data on
- `key` - Unique key for the data (recommend: "ModName_FeatureName_DataName")
- `value` - The data to store (any type)

**Example:**
```csharp
LaunchControl.Instance.Tasks.SetData(haulTask, "SmarterHauling_OpportunisticPickups", new List<string> { "item1", "item2" });
```

#### GetData<T>(Task2 task, string key)

Retrieves custom data from a Task2 instance.

**Parameters:**
- `task` - The Task2 instance to retrieve data from
- `key` - Key the data is stored under

**Returns:** The stored data, or `default(T)` if not found

**Example:**
```csharp
List<string> pickups = LaunchControl.Instance.Tasks.GetData<List<string>>(haulTask, "SmarterHauling_OpportunisticPickups");
if (pickups != null)
{
    // Use the data
}
```

#### HasData(Task2 task, string key)

Checks if a Task2 instance has data stored under a specific key.

**Parameters:**
- `task` - The Task2 instance to check
- `key` - Key to check for

**Returns:** `true` if data exists, `false` otherwise

**Example:**
```csharp
if (LaunchControl.Instance.Tasks.HasData(haulTask, "SmarterHauling_OpportunisticPickups"))
{
    // Task has our custom data
}
```

#### RemoveData(Task2 task, string key)

Removes specific data from a Task2 instance.

**Parameters:**
- `task` - The Task2 instance to remove data from
- `key` - Key of the data to remove

**Example:**
```csharp
LaunchControl.Instance.Tasks.RemoveData(haulTask, "SmarterHauling_OpportunisticPickups");
```

#### ClearTask(Task2 task)

Clears all custom data for a Task2 instance.

**Parameters:**
- `task` - The Task2 instance to clear data for

**Example:**
```csharp
LaunchControl.Instance.Tasks.ClearTask(haulTask);
```

### InteractionManager - Task Convenience Methods

Access via `LaunchControl.Instance.Interactions`

#### GetTask(Interaction interaction)

Gets the Task2 associated with an interaction.

**Parameters:**
- `interaction` - The Interaction to find the task for

**Returns:** The associated Task2, or `null` if not found

**Example:**
```csharp
Task2 task = LaunchControl.Instance.Interactions.GetTask(interaction);
if (task != null)
{
    // Use the task
}
```

#### SetTaskData<T>(Interaction interaction, string key, T value)

Sets data on the Task2 associated with an interaction. If no task is found, does nothing.

**Parameters:**
- `interaction` - The Interaction to get the task from
- `key` - Unique key for the data
- `value` - The data to store

**Example:**
```csharp
LaunchControl.Instance.Interactions.SetTaskData(interaction, "SmarterHauling_DestinationTile", 1234);
```

#### GetTaskData<T>(Interaction interaction, string key)

Gets data from the Task2 associated with an interaction.

**Parameters:**
- `interaction` - The Interaction to get the task from
- `key` - Key the data is stored under

**Returns:** The stored data, or `default(T)` if not found or no task associated

**Example:**
```csharp
int destTile = LaunchControl.Instance.Interactions.GetTaskData<int>(interaction, "SmarterHauling_DestinationTile");
```

#### HasTaskData(Interaction interaction, string key)

Checks if the Task2 associated with an interaction has data stored under a specific key.

**Parameters:**
- `interaction` - The Interaction to get the task from
- `key` - Key to check for

**Returns:** `true` if data exists, `false` otherwise or if no task associated

**Example:**
```csharp
if (LaunchControl.Instance.Interactions.HasTaskData(interaction, "SmarterHauling_DestinationTile"))
{
    // Task has our custom data
}
```

## Usage Example: Multi-Pickup Haul

This example shows how to implement opportunistic pickup during haul tasks - when an NPC picks up an item, they check for nearby items and pick up additional items until their inventory is full.

### Step 1: Create the Effect

```csharp
using Ostranauts.Bit.Interactions;
using System.Collections.Generic;

public class MultiPickupHaulEffect : IEffect
{
    public bool ShouldExecute(Interaction interaction)
    {
        // Only execute after PickupItemStack interactions during haul tasks
        return interaction.strName == "PickupItemStack" && 
               interaction.strDuty == "ACTHaulItem";
    }

    public void Execute(Interaction interaction)
    {
        // Store the original destination on first pickup
        if (!LaunchControl.Instance.Interactions.HasTaskData(interaction, "MultiPickup_OriginalDestTile"))
        {
            // Get destination from the task (assuming it's the DropItemStack interaction's target)
            Task2 task = LaunchControl.Instance.Interactions.GetTask(interaction);
            if (task != null && task.nTile >= 0)
            {
                LaunchControl.Instance.Interactions.SetTaskData(interaction, "MultiPickup_OriginalDestTile", task.nTile);
                LaunchControl.Instance.Interactions.SetTaskData(interaction, "MultiPickup_ItemsPicked", new List<string>());
            }
        }

        // Track what we just picked up
        List<string> itemsPicked = LaunchControl.Instance.Interactions.GetTaskData<List<string>>(interaction, "MultiPickup_ItemsPicked");
        if (itemsPicked != null && interaction.objThem != null)
        {
            itemsPicked.Add(interaction.objThem.strCOID);
            LaunchControl.Instance.Interactions.SetTaskData(interaction, "MultiPickup_ItemsPicked", itemsPicked);
        }

        // Check for nearby items to pick up
        CondOwner actor = interaction.objUs;
        if (actor != null)
        {
            // Get inventory space remaining
            int slotsRemaining = GetInventorySlotsRemaining(actor);
            if (slotsRemaining > 0)
            {
                // Find nearby haul tasks
                List<Task2> nearbyHaulTasks = FindNearbyHaulTasks(actor.nTile, actor.strShip);
                
                foreach (Task2 nearbyTask in nearbyHaulTasks)
                {
                    if (slotsRemaining <= 0) break;
                    
                    // Check if this task is closer to our destination than we are
                    int originalDest = LaunchControl.Instance.Interactions.GetTaskData<int>(interaction, "MultiPickup_OriginalDestTile");
                    if (IsCloserToDestination(nearbyTask.nTile, actor.nTile, originalDest))
                    {
                        // Queue a pickup for this task's item
                        QueueOpportunisticPickup(actor, nearbyTask);
                        slotsRemaining--;
                    }
                }
            }
        }
    }

    private int GetInventorySlotsRemaining(CondOwner actor)
    {
        // Implementation depends on how you track inventory
        return 5; // Example
    }

    private List<Task2> FindNearbyHaulTasks(int currentTile, string shipID)
    {
        // Search WorkManager for nearby unclaimed haul tasks
        // Implementation details omitted for brevity
        return new List<Task2>();
    }

    private bool IsCloserToDestination(int taskTile, int currentTile, int destTile)
    {
        // Calculate distances and compare
        // Implementation details omitted for brevity
        return false;
    }

    private void QueueOpportunisticPickup(CondOwner actor, Task2 nearbyTask)
    {
        // Create and queue a new PickupItemStack interaction
        // Implementation details omitted for brevity
    }

    public bool ShouldPrepare(Interaction interaction) => false;
    public void Prepare(Interaction interaction) { }
}
```

### Step 2: Register the Effect

```csharp
public class MyPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Wait for LaunchControl to initialize
        StartCoroutine(WaitForLaunchControlAndRegister());
    }

    private IEnumerator WaitForLaunchControlAndRegister()
    {
        while (LaunchControl.Instance == null)
        {
            yield return null;
        }

        // Register the effect
        LaunchControl.Instance.Interactions.RegisterEffect(new MultiPickupHaulEffect());
        Logger.LogInfo("Multi-pickup haul effect registered");
    }
}
```

## Limitations

### Hash Collisions

- Tasks with identical field values (strDuty, strInteraction, strTargetCOID, nTile, strTileShip) share the same key
- This means identical tasks will share custom data
- In practice, this is usually acceptable since identical tasks are logically the same work
- If you need to distinguish between truly identical tasks, store additional identifying information in your data

### Active vs Unclaimed Tasks

- Only **unclaimed** tasks are saved by the base game
- **Active** tasks (claimed by NPCs) are transient and not persisted
- If you need data to survive an active task's completion, store it elsewhere before the task completes

### Data Types

- All data is serialized to JSON via LitJson
- Complex types should be JSON-serializable
- When loading, data is deserialized as `JsonData` objects - you may need to convert types

## Best Practices

### Use Namespaced Keys

Always prefix your keys with your mod name to avoid conflicts:

```csharp
// Good
LaunchControl.Instance.Tasks.SetData(task, "SmarterHauling_OpportunisticPickups", data);

// Bad
LaunchControl.Instance.Tasks.SetData(task, "OpportunisticPickups", data);
```

### Clean Up Data When Done

While the system automatically cleans up data when tasks are removed, you should manually clean up data you no longer need:

```csharp
// When you're done with specific data
LaunchControl.Instance.Tasks.RemoveData(task, "MyMod_TempData");

// When you're done with all your mod's data for a task
LaunchControl.Instance.Tasks.ClearTask(task);
```

### Handle Missing Data Gracefully

Always check if data exists or handle null returns:

```csharp
// Check before using
if (LaunchControl.Instance.Tasks.HasData(task, "MyMod_Data"))
{
    int data = LaunchControl.Instance.Tasks.GetData<int>(task, "MyMod_Data");
    // Use data
}

// Or use null checking
List<string> data = LaunchControl.Instance.Tasks.GetData<List<string>>(task, "MyMod_List");
if (data != null && data.Count > 0)
{
    // Use data
}
```

### Consider Task Lifecycle

Remember that task data only exists while the Task2 exists:

```csharp
// Store data when task is created or first interaction executes
public void Execute(Interaction interaction)
{
    if (!LaunchControl.Instance.Interactions.HasTaskData(interaction, "MyMod_Initialized"))
    {
        // First time this task's interactions are executing
        LaunchControl.Instance.Interactions.SetTaskData(interaction, "MyMod_Initialized", true);
        LaunchControl.Instance.Interactions.SetTaskData(interaction, "MyMod_StartTime", Time.time);
    }
}
```

### Use Appropriate Data Types

Choose data types that serialize well:

```csharp
// Good - simple types
LaunchControl.Instance.Tasks.SetData(task, "MyMod_Count", 5);
LaunchControl.Instance.Tasks.SetData(task, "MyMod_Name", "ItemName");
LaunchControl.Instance.Tasks.SetData(task, "MyMod_List", new List<string> { "a", "b" });
LaunchControl.Instance.Tasks.SetData(task, "MyMod_Dict", new Dictionary<string, int> { ["key"] = 1 });

// Avoid - complex objects may not serialize correctly
LaunchControl.Instance.Tasks.SetData(task, "MyMod_ComplexObject", new MyComplexClass());
```

## Troubleshooting

### Data Not Persisting Across Save/Load

**Problem:** Task data is lost when loading a save.

**Solutions:**
1. Verify the task is unclaimed (only unclaimed tasks are saved by the base game)
2. Check logs for serialization errors during save
3. Ensure your data types are JSON-serializable
4. Verify PersistentData system is initialized (`LaunchControl.Instance.PersistentData` is not null)

### Can't Find Task from Interaction

**Problem:** `LaunchControl.Instance.Interactions.GetTask(interaction)` returns null.

**Solutions:**
1. Ensure you're calling this during an active interaction (in `Execute` or `Prepare`)
2. The task may have been completed already
3. The interaction may not be associated with a Task2 (some interactions are standalone)

### Data Appears on Wrong Task

**Problem:** Task data shows up on a different task than expected.

**Cause:** Hash collision - two tasks with identical fields share the same key.

**Solutions:**
1. Include additional identifying information in your data structure
2. Check if this is actually the intended behavior (identical tasks are often logically the same)
3. Consider using Interaction-level data instead if you need per-interaction uniqueness

### Memory Usage Concerns

**Problem:** Worried about memory usage with many tasks.

**Solutions:**
1. The system automatically cleans up data when tasks are removed
2. Orphaned data is cleaned up before each save
3. Only store essential data on tasks
4. Clean up temporary data manually when done with it

## See Also

- [PersistentData System](PersistentData-System.md) - The underlying save/load framework
- [Interaction System](Interaction-System.md) - Working with Interaction effects
- [Command System](Command-System.md) - Creating debug commands to inspect task data

