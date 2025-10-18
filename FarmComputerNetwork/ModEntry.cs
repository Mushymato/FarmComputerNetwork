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
    public const string ModId = "mushymato.FarmComputerNetwork";

    private static IMonitor? mon;
    private static PerScreen<RemoteViewManager> rvManagerPS = null!;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;

        rvManagerPS = new(() => new(helper));
        AssetManager.directoryPath = helper.DirectoryPath;

        helper.ConsoleCommands.Add("fcn-remoteview", "Show remote viewing for some location", ConsoleRemoteView);
        helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
        helper.Events.World.ObjectListChanged += FarmComputerWatcher.OnObjectListChanged;
        helper.Events.GameLoop.SaveLoaded += FarmComputerWatcher.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += FarmComputerWatcher.OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += FarmComputerWatcher.OnReturnedToTitle;
    }

    internal static string GetLocationDisplayName(GameLocation location)
    {
        return location.GetDisplayName() ?? location.Name;
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
        if (machine.Location == null)
            return false;

        Dictionary<string, SObject> networkedFarmComputers = FarmComputerWatcher.GetNetwork(machine, player);
        List<KeyValuePair<string, string>> responses = [];
        foreach ((string key, SObject comp) in networkedFarmComputers)
        {
            responses.Add(new(key, comp.DisplayName));
        }

        location.ShowPagedResponses(
            Game1.content.LoadString($"{AssetManager.ModStrings}:interact.question"),
            responses,
            obj =>
            {
                if (!networkedFarmComputers.TryGetValue(obj, out SObject? farmcomp))
                    return;
                location.localSound("DwarvishSentry");
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
