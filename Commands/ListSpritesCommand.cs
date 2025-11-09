using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Bit.Commands
{
    /// <summary>
    /// Command to list available sprites and resources
    /// </summary>
    public class ListSpritesCommand : MonoBehaviour
    {
        private void Start()
        {
            LaunchControl.Instance.Commands.RegisterCommand("listsprites", OnListSpritesCommand);
            LaunchControlPlugin.Logger.LogInfo("ListSprites command registered");
        }

        private void OnListSpritesCommand(string input)
        {
            try
            {
                // Parse: listsprites [keyword] [count]
                string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                string keyword = parts.Length > 1 ? parts[1].ToLower() : null;
                int maxCount = parts.Length > 2 && int.TryParse(parts[2], out int c) ? c : 50;

                LaunchControlPlugin.Logger.LogInfo("==================== Available Sprites ====================");
                
                List<string> sprites = SpriteUtility.GetAllLoadedSprites(1000); // Get more for filtering
                int count = 0;

                foreach (string spriteName in sprites)
                {
                    // Filter by keyword if provided
                    if (keyword != null && !spriteName.ToLower().Contains(keyword))
                        continue;

                    LaunchControlPlugin.Logger.LogInfo($"  - {spriteName}");
                    count++;
                    
                    if (count >= maxCount)
                    {
                        LaunchControlPlugin.Logger.LogInfo($"  ... (showing first {maxCount} matches)");
                        break;
                    }
                }

                if (count == 0)
                {
                    LaunchControlPlugin.Logger.LogInfo("  No sprites found" + (keyword != null ? $" matching '{keyword}'" : ""));
                }
                else
                {
                    LaunchControlPlugin.Logger.LogInfo($"Total: {count} sprite(s)" + (keyword != null ? $" matching '{keyword}'" : ""));
                }

                LaunchControlPlugin.Logger.LogInfo("====================================================");
                
                if (keyword == null)
                {
                    LaunchControlPlugin.Logger.LogInfo("Tip: Use 'listsprites <keyword>' to filter results");
                    LaunchControlPlugin.Logger.LogInfo("Example: listsprites gradient");
                }
            }
            catch (Exception ex)
            {
                LaunchControlPlugin.Logger.LogError($"ListSprites Error: {ex.Message}");
                LaunchControlPlugin.Logger.LogError(ex.StackTrace);
            }
        }
    }
}



