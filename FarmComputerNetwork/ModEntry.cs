global using SObject = StardewValley.Object;
using System.Diagnostics;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;

namespace FarmComputerNetwork;

public sealed class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

    public const string ModId = "mushymato.FarmComputerNetwork";
    internal const string FarmComputerId = $"{ModId}_FarmComputer";
    internal const string FarmComputerTexture = $"{FarmComputerId}/Texture";
    internal const string FarmComputerQId = $"(BC){FarmComputerId}";
    private const string FarmComputerInteractMethod =
        "FarmComputerNetwork.ModEntry, FarmComputerNetwork: InteractShowFarmComputerNetwork";
    private static IMonitor? mon;
    private static PerScreen<RemoteViewManager> rvManagerPS = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        mon = Monitor;

        rvManagerPS = new(() => new(helper));

        helper.ConsoleCommands.Add("fcn-remoteview", "Show remote viewing for some location", ConsoleRemoteView);
        helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(Edit_DataBigCraftables, AssetEditPriority.Early);
        }
        if (e.Name.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(Edit_DataMachines, AssetEditPriority.Default);
        }
        if (e.Name.IsEquivalentTo(FarmComputerTexture))
        {
            e.LoadFromModFile<Texture2D>("assets/farmcomputer.png", AssetLoadPriority.Low);
        }
    }

    private void Edit_DataBigCraftables(IAssetData asset)
    {
        IDictionary<string, BigCraftableData> data = asset.AsDictionary<string, BigCraftableData>().Data;
        BigCraftableData farmComputerClone = data["239"].ShallowClone();
        farmComputerClone.DisplayName += "+";
        farmComputerClone.Texture = FarmComputerTexture;
        farmComputerClone.SpriteIndex = 0;
        data[FarmComputerId] = farmComputerClone;
    }

    private void Edit_DataMachines(IAssetData asset)
    {
        IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
        data[FarmComputerQId] = new() { InteractMethod = FarmComputerInteractMethod };
    }

    public static bool InteractShowFarmComputerNetwork(SObject machine, GameLocation location, Farmer player)
    {
        List<KeyValuePair<string, string>> responses = [];
        Dictionary<string, SObject> foundFarmComputers = [];
        HashSet<GameLocation> processedLocations = [];

        // todo: optimize away
        Utility.ForEachLocation(location =>
        {
            GameLocation rootLocation = location.GetRootLocation();
            if (processedLocations.Contains(rootLocation))
            {
                return true;
            }
            foreach (SObject locMachine in rootLocation.Objects.Values)
            {
                if (locMachine.GetMachineData()?.InteractMethod == FarmComputerInteractMethod)
                {
                    string key = $"FC_{location.NameOrUniqueName}_{locMachine.TileLocation}";
                    responses.Add(
                        new(key, $"{location.DisplayName}({locMachine.TileLocation.X}, {locMachine.TileLocation.Y})")
                    );
                    foundFarmComputers[key] = locMachine;
                    Log(key);
                }
            }
            processedLocations.Add(rootLocation);
            return true;
        });

        location.ShowPagedResponses(
            I18n.Interact_Question(),
            responses,
            obj =>
            {
                if (!foundFarmComputers.TryGetValue(obj, out SObject? farmcomp))
                {
                    return;
                }
                rvManagerPS.Value.BeginRemoteView(farmcomp, player);
            },
            auto_select_single_choice: true
        );

        return true;
    }

    internal static void ConsoleRemoteView(string cmd, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Log("Save not loaded");
            return;
        }
        if (!ArgUtility.TryGetOptional(args, 0, out string locationName, out string error, name: "string locationName"))
        {
            Log(error, LogLevel.Error);
            return;
        }
        GameLocation location = locationName != null ? Game1.RequireLocation(locationName) : Game1.currentLocation;
        SObject dummyObject = ItemRegistry.Create<SObject>("(BC)239");
        dummyObject.Location = location;
        dummyObject.TileLocation = Vector2.Zero;
        rvManagerPS.Value.BeginRemoteView(dummyObject, Game1.player);
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }

    /// <summary>SMAPI static monitor Log wrapper, debug only</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    [Conditional("DEBUG")]
    internal static void LogDebug(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }
}
