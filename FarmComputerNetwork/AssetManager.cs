using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Machines;

namespace FarmComputerNetwork;

internal class AssetManager
{
    internal const string FarmComputerId = $"{ModEntry.ModId}_FarmComputer";
    internal const string FarmComputerQId = $"(BC){FarmComputerId}";
    internal const string FarmComputerTexture = $"{FarmComputerId}/Texture";
    internal const string FarmComputerInteractMethod =
        "FarmComputerNetwork.ModEntry, FarmComputerNetwork: InteractShowFarmComputerNetwork";
    internal const string ModStrings = $"{ModEntry.ModId}/Strings";

    internal static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(Edit_DataBigCraftables, AssetEditPriority.Default);
        }
        else if (e.Name.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(Edit_DataMachines, AssetEditPriority.Default);
        }
        else if (e.Name.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(Edit_DataCraftingRecipes, AssetEditPriority.Default);
        }
        else if (e.Name.IsEquivalentTo("Data/TriggerActions"))
        {
            e.Edit(Edit_DataTriggerActions, AssetEditPriority.Default);
        }
        else if (e.Name.IsEquivalentTo(FarmComputerTexture))
        {
            e.LoadFromModFile<Texture2D>("assets/farmcomputer.png", AssetLoadPriority.Low);
        }
        else if (e.Name.IsEquivalentTo(ModStrings))
        {
            string stringsAsset = Path.Combine("i18n", e.Name.LanguageCode?.ToString() ?? "default", "strings.json");
            if (File.Exists(stringsAsset))
            {
                e.LoadFromModFile<Dictionary<string, string>>(stringsAsset, AssetLoadPriority.Low);
            }
            else
            {
                e.LoadFromModFile<Dictionary<string, string>>("i18n/default/strings.json", AssetLoadPriority.Low);
            }
        }
    }

    private static void Edit_DataBigCraftables(IAssetData asset)
    {
        IDictionary<string, BigCraftableData> data = asset.AsDictionary<string, BigCraftableData>().Data;
        data[FarmComputerId] = new()
        {
            Name = FarmComputerId,
            DisplayName = $"[LocalizedText {ModStrings}:bc.displayname]",
            Description = $"[LocalizedText {ModStrings}:bc.description]",
            Price = 1000,
            Fragility = 0,
            CanBePlacedOutdoors = true,
            CanBePlacedIndoors = true,
            IsLamp = true,
            Texture = FarmComputerTexture,
            SpriteIndex = 0,
        };
    }

    private static void Edit_DataMachines(IAssetData asset)
    {
        IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
        data[FarmComputerQId] = new()
        {
            InteractMethod = FarmComputerInteractMethod,
            WobbleWhileWorking = false,
            AllowFairyDust = false,
            WorkingEffects = [new() { Frames = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], Interval = 100 }],
        };
    }

    private static void Edit_DataCraftingRecipes(IAssetData asset)
    {
        IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
        data[FarmComputerQId] = $"(BC)239 1 82 10/Home/{FarmComputerQId}/true/null/";
    }

    private static void Edit_DataTriggerActions(IAssetData asset)
    {
        IList<TriggerActionData> data = asset.GetData<IList<TriggerActionData>>();
        data.Add(
            new()
            {
                Id = $"{FarmComputerQId}_Unlock",
                Trigger = "DayStarted",
                Condition = "PLAYER_HAS_MAIL Current DemetriusReward",
                Action = $"MarkCraftingRecipeKnown Current {FarmComputerQId}",
            }
        );
    }
}
