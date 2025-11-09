# Command System

The command system lets you register custom console commands that you can use in-game for debugging, testing, or just making your modding life easier.

## Why Commands?

Ever wanted to test a specific feature without restarting the game? Commands are your friend. They let you:
- Test functions on the fly
- Toggle debug features
- Inspect game state
- Trigger events for testing

## Basic Usage

### Registering a Command

```csharp
using Ostranauts.Bit;

public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        // Register your command
        LaunchControl.RegisterCommand("mycommand", OnMyCommand);
    }

    private void OnMyCommand(string input)
    {
        // This gets called when the user types "mycommand" in console
        Debug.Log("Hello from my command!");
    }
}
```

### Using Commands In-Game

1. Open the console (usually the `` ` `` key)
2. Type your command: `mycommand`
3. Press Enter

That's it!

## Command Arguments

Your callback receives the full input string, so you can parse arguments however you want:

```csharp
private void OnMyCommand(string input)
{
    // input will be something like "mycommand arg1 arg2 arg3"
    
    string[] parts = input.Split(' ');
    
    if (parts.Length < 2)
    {
        Debug.Log("Usage: mycommand <argument>");
        return;
    }
    
    string arg = parts[1];
    Debug.Log($"You passed: {arg}");
}
```

## Example: Toggle Command

```csharp
private bool _debugMode = false;

private void Awake()
{
    LaunchControl.RegisterCommand("debugmode", OnToggleDebug);
}

private void OnToggleDebug(string input)
{
    _debugMode = !_debugMode;
    Debug.Log($"Debug mode: {(_debugMode ? "ON" : "OFF")}");
}
```

## Example: Command with Parameters

```csharp
private void Awake()
{
    LaunchControl.RegisterCommand("spawn", OnSpawnCommand);
}

private void OnSpawnCommand(string input)
{
    // Parse: spawn <itemName> <count>
    string[] parts = input.Split(' ');
    
    if (parts.Length < 3)
    {
        Debug.Log("Usage: spawn <itemName> <count>");
        return;
    }
    
    string itemName = parts[1];
    if (int.TryParse(parts[2], out int count))
    {
        SpawnItem(itemName, count);
    }
    else
    {
        Debug.Log("Count must be a number!");
    }
}

private void SpawnItem(string name, int count)
{
    Debug.Log($"Spawning {count}x {name}");
    // Your spawn logic here
}
```

## Built-in Commands

LaunchControl comes with some useful built-in commands:

### `dumpui`
Dumps the UI hierarchy of whatever you click on. Super useful for finding sprites and understanding UI structure.

See the [DumpUI Command](DumpUI-Command.md) page for details.

### `listsprites`
Lists all available sprites, with optional keyword filtering.

**Usage:**
```
# List first 50 sprites
listsprites

# Filter by keyword
listsprites gradient

# Get more results
listsprites gradient 100
```

Essential for discovering what sprites are available when using [Sprite Utility](Sprite-Utility.md)!

## Tips

### Command Names
- Use lowercase names (they're case-insensitive, but lowercase looks cleaner)
- Keep them short and memorable
- Prefix with your mod name if you're worried about conflicts: `mymod_command`

### Error Handling
Always validate your input! Users will type weird stuff.

```csharp
private void OnMyCommand(string input)
{
    try
    {
        // Your command logic
    }
    catch (Exception ex)
    {
        Debug.LogError($"Command error: {ex.Message}");
    }
}
```

### Help Text
Print usage info when commands are called incorrectly:

```csharp
if (parts.Length < 2)
{
    Debug.Log("Usage: mycommand <required> [optional]");
    Debug.Log("Example: mycommand foo");
    return;
}
```

## API Reference

### `LaunchControl.RegisterCommand(string name, Action<string> callback)`

Registers a console command.

**Parameters:**
- `name` - The command name (case-insensitive)
- `callback` - Function to call when command is executed. Receives the full input string.

**Example:**
```csharp
LaunchControl.RegisterCommand("test", (input) => {
    Debug.Log("Test command called!");
});
```

## Common Patterns

### Command with Subcommands

```csharp
private void OnConfigCommand(string input)
{
    string[] parts = input.Split(' ');
    
    if (parts.Length < 2)
    {
        Debug.Log("Usage: config <set|get|reset> [args]");
        return;
    }
    
    string subcommand = parts[1].ToLower();
    
    switch (subcommand)
    {
        case "set":
            ConfigSet(parts);
            break;
        case "get":
            ConfigGet(parts);
            break;
        case "reset":
            ConfigReset();
            break;
        default:
            Debug.Log($"Unknown subcommand: {subcommand}");
            break;
    }
}
```

### Command with Feedback

```csharp
private void OnClearCache(string input)
{
    int itemsCleared = ClearMyCache();
    Debug.Log($"Cache cleared: {itemsCleared} items removed");
}
```

## Troubleshooting

**Command not working?**
- Check the logs - LaunchControl logs when commands are registered
- Make sure you're calling `RegisterCommand` during `Awake()` or `Start()`
- Verify the command name doesn't have spaces or special characters

**Command callback not firing?**
- Check for exceptions in your callback
- Make sure your plugin is actually loading (check BepInEx logs)

**Want to see registered commands?**
Check the LaunchControl logs on startup - it prints all registered commands.

---

[← Back to Home](Home.md) | [Sprite Utility →](Sprite-Utility.md)

