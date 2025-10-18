global using SObject = StardewValley.Object;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace FarmComputerNetwork;

public sealed class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    internal const string FarmComputerInteractMethod =
        "FarmComputerNetwork.ModEntry, FarmComputerNetwork: InteractShowFarmComputerNetwork";
    public const string ModId = "mushymato.FarmComputerNetwork";

    private static IMonitor? mon;
    private static PerScreen<RemoteViewManager> rvManagerPS = null!;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;

        rvManagerPS = new(() => new(helper));

        helper.ConsoleCommands.Add("fcn-remoteview", "Show remote viewing for some location", ConsoleRemoteView);
        helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
    }

    /// <summary>
    /// This is called via 'FarmComputerNetwork.ModEntry, FarmComputerNetwork: InteractShowFarmComputerNetwork'
    /// </summary>
    /// <param name="machine"></param>
    /// <param name="location"></param>
    /// <param name="player"></param>
    /// <returns></returns>
    public static bool InteractShowFarmComputerNetwork(SObject machine, GameLocation location, Farmer player)
    {
        List<KeyValuePair<string, string>> responses = [];
        Dictionary<string, SObject> foundFarmComputers = [];

        // todo: optimize away
        Utility.ForEachLocation(location =>
        {
            foreach (SObject locMachine in location.Objects.Values)
            {
                if (locMachine.GetMachineData()?.InteractMethod == FarmComputerInteractMethod)
                {
                    string key = $"FC_{location.NameOrUniqueName}_{locMachine.TileLocation}";
                    responses.Add(
                        new(key, $"{location.DisplayName} ({locMachine.TileLocation.X}, {locMachine.TileLocation.Y})")
                    );
                    foundFarmComputers[key] = locMachine;
                    Log(key);
                }
            }
            return true;
        });

        location.ShowPagedResponses(
            Game1.content.LoadString($"{AssetManager.ModStrings}:interact.question"),
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
