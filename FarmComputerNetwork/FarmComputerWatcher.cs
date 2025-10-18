using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace FarmComputerNetwork;

internal static class FarmComputerWatcher
{
    private static readonly HashSet<SObject> knownFarmComputers = [];

    private const string ModData_FarmComputerName = $"{ModEntry.ModId}/FarmComputerName";

    internal static bool IsFarmComputerPP(this SObject comp)
    {
        return comp.GetMachineData()?.InteractMethod == AssetManager.FarmComputerInteractMethod;
    }

    private static void SetWorkingState(SObject comp)
    {
        comp.MinutesUntilReady = 99999;
        comp.readyForHarvest.Value = false;
        comp.heldObject.Value = comp.getOne() as SObject;
        if (comp.modData.TryGetValue(ModData_FarmComputerName, out string displayName))
            comp.displayNameFormat = displayName;
        else if (comp.Location != null)
            comp.displayNameFormat = ModEntry.GetLocationDisplayName(comp.Location);
    }

    public static void NameComputer(SObject comp, string name)
    {
        if (name.Length <= 0)
        {
            return;
        }

        comp.modData[ModData_FarmComputerName] = name;
        comp.displayNameFormat = name;

        Game1.playSound("DwarvishSentry");
        Game1.exitActiveMenu();
    }

    internal static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Utility.ForEachLocation(location =>
        {
            foreach (SObject locMachine in location.Objects.Values)
            {
                if (locMachine.IsFarmComputerPP())
                {
                    knownFarmComputers.Add(locMachine);
                    SetWorkingState(locMachine);
                }
            }
            return true;
        });
    }

    internal static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        foreach (SObject comp in knownFarmComputers)
        {
            SetWorkingState(comp);
        }
    }

    internal static void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        foreach ((_, SObject removed) in e.Removed)
        {
            knownFarmComputers.Remove(removed);
        }

        foreach ((_, SObject added) in e.Added)
        {
            if (added.IsFarmComputerPP())
            {
                knownFarmComputers.Add(added);
                NamingMenu namingMenu =
                    new(
                        name => NameComputer(added, name),
                        Game1.content.LoadString($"{AssetManager.ModStrings}:naming.question"),
                        ModEntry.GetLocationDisplayName(added.Location)
                    );
                if (Game1.activeClickableMenu != null)
                    Game1.nextClickableMenu.Add(namingMenu);
                else
                    Game1.activeClickableMenu = namingMenu;
                SetWorkingState(added);
            }
        }
    }

    internal static void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        knownFarmComputers.Clear();
    }

    internal static Dictionary<string, SObject> GetNetwork(SObject farmComputer, Farmer player)
    {
        Dictionary<string, SObject> networkedFarmComputers = [];
        networkedFarmComputers[$"FC_{farmComputer.Location.NameOrUniqueName}_{farmComputer.TileLocation}"] =
            farmComputer;
        foreach (SObject comp in knownFarmComputers)
        {
            if (comp.Location == null)
                continue;
            networkedFarmComputers[$"FC_{comp.Location.NameOrUniqueName}_{comp.TileLocation}"] = comp;
        }
        return networkedFarmComputers;
    }
}
