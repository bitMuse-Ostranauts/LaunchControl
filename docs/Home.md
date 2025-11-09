# LaunchControl Wiki

## What is LaunchControl?

LaunchControl is a core library that provides common functionality for Ostranauts mods.
---

## 📚 Documentation Index

### Core Features
- **[Command System](Command-System.md)** - Register custom console commands
- **[MegaTooltip System](MegaTooltip-System.md)** - Add custom modules to item/person tooltips
- **[Pledge System](Pledge-System.md)** - Register custom AI behaviors/pledges
- **[Task System](Task-System.md)** - Store custom metadata on Task2 instances with save/load support
- **[PersistentData System](PersistentData-System.md)** - Save and load custom mod data with game saves
- **[Sprite Utility](Sprite-Utility.md)** - Load sprites by keywords
- **[DumpUI Command](DumpUI-Command.md)** - Explore the game's UI hierarchy

### Built-in Commands
- `dumpui` - Click UI elements to see their hierarchy and components
- `listsprites` - List all available sprites, with optional keyword filtering

---

## 🤔 Common Questions

### Getting Started

**Q: How do I add LaunchControl to my mod?**  
→ [See: Setting up LaunchControl](#setting-up-LaunchControl)

**Q: What namespace do I need to import?**  
→ Add `using Ostranauts.Bit;` to your files

**Q: Do I need to reference LaunchControl in my .csproj?**  
→ [See: Project Setup](#project-setup)

### Commands

**Q: How do I create a custom console command?**  
→ [Command System - Basic Usage](Command-System.md#basic-usage)

**Q: How do I parse command arguments?**  
→ [Command System - Command Arguments](Command-System.md#command-arguments)

**Q: What built-in commands are available?**  
→ [DumpUI Command](DumpUI-Command.md)

### Pledges

**Q: How do I create custom AI behaviors?**  
→ [Pledge System - Basic Registration](Pledge-System.md#basic-registration)

**Q: How do I register a custom pledge type?**  
→ [Pledge System - Complete Example](Pledge-System.md#complete-example)

**Q: What is a pledge and how does it work?**  
→ [Pledge System - Overview](Pledge-System.md#overview)

### MegaTooltip Modules

**Q: How do I add custom UI to tooltips?**  
→ [MegaTooltip System - Quick Start](MegaTooltip-System.md#quick-start)

**Q: How do I control when my module appears?**  
→ [MegaTooltip System - OnlyShowIf](MegaTooltip-System.md#onlyshowiffunccondownerbool-predicate)

**Q: How do I position my module in the tooltip?**  
→ [MegaTooltip System - InsertAfter/InsertBefore](MegaTooltip-System.md#insertafterstring-modulename)

**Q: My module isn't appearing, what's wrong?**  
→ [MegaTooltip System - Troubleshooting](MegaTooltip-System.md#troubleshooting)

### Tasks

**Q: How do I store custom data on Task2 instances?**  
→ [Task System - Basic Usage](Task-System.md#direct-task-access)

**Q: How do I access task data from an Interaction?**  
→ [Task System - Access from Interaction Context](Task-System.md#access-from-interaction-context)

**Q: Does task data persist across save/load?**  
→ Yes! See [Task System - Save/Load Flow](Task-System.md#saveload-flow)

**Q: How do I implement multi-pickup haul behavior?**  
→ [Task System - Multi-Pickup Haul Example](Task-System.md#usage-example-multi-pickup-haul)

### Persistent Data

**Q: How do I save custom mod data with game saves?**  
→ [PersistentData System - How to Use](PersistentData-System.md#how-to-use)

**Q: What types of data can I save?**  
→ [PersistentData System - Architecture](PersistentData-System.md#architecture)

### Sprites

**Q: How do I find and use game sprites?**  
→ [Sprite Utility - Basic Usage](Sprite-Utility.md#basic-usage)

**Q: How do I discover what sprites are available?**  
→ Use the `listsprites` command in-game, or see [Discovering Available Sprites](Sprite-Utility.md#discovering-available-sprites)

**Q: How do I know what keywords to use?**  
→ [Sprite Utility - Finding the Right Keywords](Sprite-Utility.md#finding-the-right-keywords)

**Q: Why is my sprite not loading?**  
→ [Sprite Utility - Troubleshooting](Sprite-Utility.md#troubleshooting)

### UI Exploration

**Q: How do I explore the game's UI?**  
→ [DumpUI Command - How to Use](DumpUI-Command.md#how-to-use)

**Q: How do I find sprite names?**  
→ [DumpUI Command - Finding Sprites](DumpUI-Command.md#use-case-1-finding-sprites)

**Q: How do I understand UI structure?**  
→ [DumpUI Command - Reading Output](DumpUI-Command.md#reading-the-output)

---

## ⚙️ Setup Guides

### Setting up LaunchControl

LaunchControl should be installed in your game's BepInEx plugins folder. If you're using it as a modder, you just need to reference it.

### Project Setup

Add this to your mod's `.csproj` file:

```xml
<ItemGroup>
  <Reference Include="LaunchControl">
    <HintPath>$(GameDir)\BepInEx\plugins\LaunchControl\LaunchControl.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>
```

Replace `$(GameDir)` with your game directory path or variable.

---

## 🔧 Feature Overview

### Command System
Create custom console commands for debugging and testing. Commands are registered once and available throughout your gameplay session.

→ [Full Documentation](Command-System.md)

### MegaTooltip System
Add custom data modules to item and person tooltips with a fluent registration API. Automatically handles template creation, caching, and positioning.

→ [Full Documentation](MegaTooltip-System.md)

### Pledge System
Register custom AI behaviors (pledges) for characters. Create new goal-driven behaviors without modifying game files or writing complex Harmony patches.

→ [Full Documentation](Pledge-System.md)

### Task System
Store custom metadata on Task2 instances that persists across save/load cycles. Access task data from Interaction context for advanced AI behavior modifications.

→ [Full Documentation](Task-System.md)

### PersistentData System
Save and load custom mod data automatically with game saves. Thread-safe, error-isolated framework for mod data persistence.

→ [Full Documentation](PersistentData-System.md)

### Sprite Utility
Load sprites from game prefabs with a simple API. Automatically caches results for performance.

→ [Full Documentation](Sprite-Utility.md)

### DumpUI Command
Click any UI element and see its complete hierarchy, components, and sprites. Essential for UI modding.

→ [Full Documentation](DumpUI-Command.md)

---

## 💡 Philosophy

LaunchControl tries to make common modding tasks simpler without getting in your way. If you find yourself writing the same code across multiple mods, it probably belongs in LaunchControl.

---

## 🚀 Quick Links

- [How to register a command](Command-System.md#registering-a-command)
- [How to add a tooltip module](MegaTooltip-System.md#quick-start)
- [How to register a custom pledge](Pledge-System.md#basic-registration)
- [How to store task metadata](Task-System.md#direct-task-access)
- [How to save custom mod data](PersistentData-System.md#how-to-use)
- [How to load a sprite](Sprite-Utility.md#basic-usage)
- [How to explore UI](DumpUI-Command.md#how-to-use)
- [Troubleshooting sprite loading](Sprite-Utility.md#troubleshooting)
- [Reading DumpUI output](DumpUI-Command.md#reading-the-output)

