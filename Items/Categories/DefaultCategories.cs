using System;

namespace Ostranauts.Bit.Items.Categories
{
    /// <summary>
    /// Default item categories - currently empty, to be populated by mods
    /// </summary>
    public static class DefaultCategories
    {
        public static void Initialize(ItemCategoryManager manager)
        {
            LaunchControlPlugin.Logger.LogInfo("Initializing default categories...");

            // Create Food category as root category
            var foodCategory = new ItemCategory("Food", "Food", "Edible items including meals, ingredients, and rations.");
            manager.AddCategory(foodCategory);

            // Add a "Meals" subcategory under Food for prepared food items
            var mealsCategory = new ItemCategory("Meals", "Meals", "Prepared meals and cooked food.");
            mealsCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;

                // Check if the item has an "IsFoodPrepared" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("isfoodprepared"))
                    {
                        return true;
                    }
                }

                return false;
            });
            manager.AddCategory(mealsCategory, "Food");

            // Add a "Future Food" subcategory under Food for trencher items
            var futureFoodCategory = new ItemCategory("Future Food", "Future Food", "Prepared meals and futuristic edible items.");
            futureFoodCategory.AddPredicate(co => {
                if (co == null) return false;

                // Get item ID
                string itemId = co.strCODef ?? co.strItemDef;
                if (string.IsNullOrEmpty(itemId)) return false;

                // Get item name (fallback to ID if no name)
                string itemName = co.strName ?? itemId;

                // Check if item name or ID contains "trencher" (case-insensitive)
                string itemNameLower = itemName.ToLower();
                string itemIdLower = itemId.ToLower();

                return itemNameLower.Contains("trencher") || itemIdLower.Contains("trencher");
            });
            manager.AddCategory(futureFoodCategory, "Food");

            // Create Medical category as root category
            var medicalCategory = new ItemCategory("Medical", "Medical", "Medical supplies and consumables such as pills, bandages, and first aid kits.");
            manager.AddCategory(medicalCategory);

            // Pills subcategory with predicate detecting "pill" in name and PartsSmall cond
            var pillsCategory = new ItemCategory("Pills", "Pills", "Pills, capsules, and related medicine.");
            pillsCategory.AddPredicate(co =>
            {
                if (co == null) return false;

                // Check name/id
                string name = (co.strName ?? co.strCODef ?? co.strItemDef ?? "").ToLower();
                if (!name.Contains("pill"))
                    return false;

                // Check for PartsSmall cond (cond names)
                if (co.aStartingConds != null)
                {
                    for (int i = 0; i < co.aStartingConds.Length; i++)
                    {
                        if (co.aStartingConds[i] != null && co.aStartingConds[i].ToLower().Contains("partssmall"))
                        {
                            return true;
                        }
                    }
                }

                return false;
            });
            manager.AddCategory(pillsCategory, "Medical");

            // Injections subcategory for injection pens
            var injectionsCategory = new ItemCategory("Injections", "Injections", "Injectable medicines and pens.");
            injectionsCategory.AddItem("ItmOssifexPen01");
            injectionsCategory.AddItem("ItmAntiGravPen01");
            injectionsCategory.AddItem("ItmAntiRadPen01");
            manager.AddCategory(injectionsCategory, "Medical");

            // Smokes subcategory for cigarettes and smoking items
            var smokesCategory = new ItemCategory("Smokes", "Smokes", "Cigarettes and smoking items.");
            smokesCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;

                // Check if the item has an "IsCigarette" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("iscigarette"))
                    {
                        return true;
                    }
                }

                return false;
            });
            manager.AddCategory(smokesCategory, "Medical");

            var junkCategory = new ItemCategory("Junk", "Junk", "Scrap and junk often used as components for repairs.");
            junkCategory.AddItem("ItmScrapAluminum");
            junkCategory.AddItem("ItmScrapCarbonFiber");
            junkCategory.AddItem("ItmScrapClothClean");
            junkCategory.AddItem("ItmScrapClothDirty");
            junkCategory.AddItem("ItmScrapSteel");
            junkCategory.AddItem("ItmPartsScreen01");
            junkCategory.AddItem("ItmComponentMotor01");
            junkCategory.AddItem("ItmHeatSink01");
            junkCategory.AddItem("ItmComponentMobo01");
            junkCategory.AddItem("ItmPartsMechSmall01");
            junkCategory.AddItem("ItmPartsElecSmall01");
            manager.AddCategory(junkCategory);
            
            var toolsCategory = new ItemCategory("Tools", "Tools", "Tools used for repairs and construction.");
            toolsCategory.AddItem("ItmToolFireEx01", "ItmToolFireEx01Dmg");
            toolsCategory.AddItem("ItmToolWelder01", "ItmToolWelder01Dmg");
            toolsCategory.AddItem("ItmToolHacksaw01");
            toolsCategory.AddItem("ItmToolSolderingIron01", "ItmToolSolderingIron01Dmg");
            toolsCategory.AddItem("ItmToolDrill01", "ItmToolDrill01Dmg");
            toolsCategory.AddItem("ItmToolGrinder02", "ItmToolGrinder02Dmg");
            toolsCategory.AddItem("ItmToolCrowbar01");
            toolsCategory.AddItem("ItmToolLaserTorch01", "ItmToolLaserTorch01Dmg");
            manager.AddCategory(toolsCategory);

            var equipBatteriesCategory = new ItemCategory("Equipment Batteries", "Equipment Batteries", "Batteries used for tools and gear.");
            equipBatteriesCategory.AddItem("ItmBatteryDisp01");
            equipBatteriesCategory.AddItem("ItmBattery03", "ItmBattery03Dmg");
            equipBatteriesCategory.AddItem("ItmBatteryDrill01", "ItmBatteryDrill01Dmg");
            equipBatteriesCategory.AddItem("ItmBatteryWelder01", "ItmBatteryWelder01Dmg");
            equipBatteriesCategory.AddItem("ItmBattery04", "ItmBattery04Dmg");
            manager.AddCategory(equipBatteriesCategory);

            var weaponsCategory = new ItemCategory("Weapons", "Weapons", "Weapons and combat items.");
            manager.AddCategory(weaponsCategory);

            var meleeCategory = new ItemCategory("Melee", "Melee", "Melee weapons for close combat.");
            meleeCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;

                // Check if the item has an "IsWeaponMelee" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("isweaponmelee"))
                    {
                        return true;
                    }
                }

                return false;
            });
            manager.AddCategory(meleeCategory, "Weapons");

            var rangedCategory = new ItemCategory("Ranged", "Ranged", "Ranged weapons for distance combat.");
            rangedCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;

                // Check if the item has an "IsWeaponRanged" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("isweaponranged"))
                    {
                        return true;
                    }
                }

                return false;
            });
            manager.AddCategory(rangedCategory, "Weapons");

            var ammoCategory = new ItemCategory("Ammo", "Ammo", "Ammunition for ranged weapons.");
            ammoCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;

                bool hasIsAmmo = false;
                bool hasIsCategoryWeapons = false;

                // Check if the item has both "IsAmmo" and "IsCategoryWeapons" conditions
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null)
                    {
                        string condLower = cond.ToLower();
                        if (condLower.Contains("isammo"))
                        {
                            hasIsAmmo = true;
                        }
                        if (condLower.Contains("iscategoryweapons"))
                        {
                            hasIsCategoryWeapons = true;
                        }
                    }
                }

                return hasIsAmmo && hasIsCategoryWeapons;
            });
            manager.AddCategory(ammoCategory, "Weapons");
            

            var clothingCategory = new ItemCategory("Clothing", "Clothing", "Items worn by the crew.");
            manager.AddCategory(clothingCategory);
            
            var jumpsuitsCategory = new ItemCategory("Jumpsuits", "Jumpsuits", "Jumpsuits and similar outfits worn by crew.");
            jumpsuitsCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;
                
                // Check if the item has a "jumpsuit" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("jumpsuit"))
                    {
                        return true;
                    }
                }
                
                return false;
            });

            var shirtsCategory = new ItemCategory("Shirts", "Shirts", "Shirts and similar clothing worn by crew.");
            shirtsCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;
                
                // Check if the name contains "shirt"
                string itemName = co.strName ?? "";
                if (string.IsNullOrEmpty(itemName) || !itemName.ToLower().Contains("shirt"))
                {
                    return false;
                }
                
                // Check if the item has an "IsCategoryTextiles" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("iscategorytextiles"))
                    {
                        return true;
                    }
                }
                
                return false;
            });

            var pantsCategory = new ItemCategory("Pants", "Pants", "Pants, leggings, and similar clothing worn by crew.");
            pantsCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;
                
                // Check if the name contains "pants" or "leggings"
                string itemName = co.strName ?? "";
                if (string.IsNullOrEmpty(itemName))
                {
                    return false;
                }
                
                string itemNameLower = itemName.ToLower();
                if (!itemNameLower.Contains("pants") && !itemNameLower.Contains("leggings"))
                {
                    return false;
                }
                
                // Check if the item has an "IsCategoryTextiles" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("iscategorytextiles"))
                    {
                        return true;
                    }
                }
                
                return false;
            });

            var footwearCategory = new ItemCategory("Footwear", "Footwear", "Shoes, boots, and other footwear worn by crew.");
            footwearCategory.AddPredicate(co => {
                if (co == null || co.aStartingConds == null) return false;
                
                // Check if the item has an "IsShoe" condition
                for (int i = 0; i < co.aStartingConds.Length; i++)
                {
                    string cond = co.aStartingConds[i];
                    if (cond != null && cond.ToLower().Contains("isshoe"))
                    {
                        return true;
                    }
                }
                
                return false;
            });
            
            manager.AddCategory(jumpsuitsCategory, "Clothing");
            manager.AddCategory(shirtsCategory, "Clothing");
            manager.AddCategory(pantsCategory, "Clothing");
            manager.AddCategory(footwearCategory, "Clothing");

            LaunchControlPlugin.Logger.LogInfo("Default categories initialization complete");
        }
    }
}
