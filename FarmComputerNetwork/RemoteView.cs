using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace FarmComputerNetwork;

internal sealed class RemoteView(
    SObject farmComputer,
    Farmer player,
    GameLocation priorLocation,
    xTile.Dimensions.Location priorViewport
) : IDisposable
{
    private bool isViewing = false;
    private double? panningSince = null;
    private double panningSpeedFactor = 1.0;
    private bool prevDisplayHUD = false;
    private Point prevTilePoint = Point.Zero;
    private int prevFacing = 0;

    public void Dispose()
    {
        if (isViewing)
        {
            StopViewing();
        }
    }

    internal bool BeginViewing()
    {
        if (isViewing)
        {
            ModEntry.Log("already viewing location", LogLevel.Error);
            return false;
        }
        if (farmComputer == null || farmComputer.Location == null)
        {
            ModEntry.Log("farm computer location is null", LogLevel.Error);
            return false;
        }

        GameLocation viewLocation = farmComputer.Location;

        ModEntry.Log($"Remote view {farmComputer.Location}");

        prevTilePoint = player.TilePoint;
        prevFacing = player.FacingDirection;

        Game1.currentLocation = viewLocation;
        player.viewingLocation.Value = viewLocation.NameOrUniqueName;
        Game1.currentLocation.resetForPlayerEntry();
        Game1.globalFadeToClear();
        Game1.viewportFreeze = true;

        Game1.viewport.Location = new xTile.Dimensions.Location(
            (int)(farmComputer.TileLocation.X * 64 - Game1.viewport.Width / 2f),
            (int)(farmComputer.TileLocation.Y * 64 - Game1.viewport.Height / 2f)
        );

        Game1.clampViewportToGameMap();
        Game1.panScreen(0, 0);
        Game1.displayFarmer = false;
        prevDisplayHUD = Game1.displayHUD;
        Game1.displayHUD = false;

        Game1.activeClickableMenu = new FarmComputerInfoBox(farmComputer);

        isViewing = true;
        return true;
    }

    internal void StopViewing()
    {
        ModEntry.Log($"Stop remote view {Game1.currentLocation.NameOrUniqueName}");

        LocationRequest locationRequest = Game1.getLocationRequest(priorLocation.NameOrUniqueName);

        locationRequest.OnWarp += () =>
        {
            player.setTileLocation(prevTilePoint.ToVector2());
            player.faceDirection(prevFacing);
            prevTilePoint = Point.Zero;
            prevFacing = 0;
            player.viewingLocation.Value = null;
            Game1.viewportFreeze = false;
            Game1.viewport.Location = priorViewport;
            Game1.displayFarmer = true;
            Game1.displayHUD = prevDisplayHUD;
        };

        isViewing = false;
        panningSince = null;

        Game1.warpFarmer(locationRequest, prevTilePoint.X, prevTilePoint.Y, prevFacing);
    }

    internal bool Update(GameTime time)
    {
        if (!isViewing)
            return false;

        if (farmComputer.Location is not FarmHouse && farmComputer.Location != priorLocation)
        {
            farmComputer.updateWhenCurrentLocation(time);
        }

        if (Game1.IsFading())
            return false;

        if (Game1.activeClickableMenu is null)
        {
            StopViewing();
            return true;
        }

        int panX = 0;
        int panY = 0;

        Keys[] pressedKeys = Game1.oldKBState.GetPressedKeys();
        foreach (Keys key in pressedKeys)
        {
            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                StopViewing();
                return true;
            }
            if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
            {
                panY += 4;
            }
            else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
            {
                panX += 4;
            }
            else if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
            {
                panY -= 4;
            }
            else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
            {
                panX -= 4;
            }
        }

        int mouseXDelta = Game1.getOldMouseX(ui_scale: false) + Game1.viewport.X;
        int mouseYDelta = Game1.getOldMouseY(ui_scale: false) + Game1.viewport.Y;
        if (mouseXDelta - Game1.viewport.X < 64)
        {
            panX -= 8;
        }
        else if (mouseXDelta - (Game1.viewport.X + Game1.viewport.Width) >= -128)
        {
            panX += 8;
        }
        if (mouseYDelta - Game1.viewport.Y < 64)
        {
            panY -= 8;
        }
        else if (mouseYDelta - (Game1.viewport.Y + Game1.viewport.Height) >= -64)
        {
            panY += 8;
        }

        if (panX != 0 || panY != 0)
        {
            panningSince ??= time.TotalGameTime.TotalMilliseconds;
            panningSpeedFactor = Math.Min(3, 1 + (time.TotalGameTime.TotalMilliseconds - panningSince.Value) / 1000f);
            bool priorPaused = Game1.netWorldState.Value.IsPaused;
            Game1.netWorldState.Value.IsPaused = true;
            Game1.panScreen((int)(panX * panningSpeedFactor), (int)(panY * panningSpeedFactor));
            Game1.netWorldState.Value.IsPaused = priorPaused;
        }
        else
        {
            panningSince = null;
        }

        return false;
    }
}

internal class RemoteViewManager
{
    internal IModHelper helper;
    private RemoteView? currentRemoteView = null;
    private RemoteView? CurrentRemoteView
    {
        get => currentRemoteView;
        set
        {
            // viewing -> not viewing
            if (currentRemoteView != null && value == null)
            {
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
                currentRemoteView = null;
            }
            // not viewing -> viewing
            else if (currentRemoteView == null && value != null && value.BeginViewing())
            {
                currentRemoteView = value;
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }
    }

    internal RemoteViewManager(IModHelper helper)
    {
        this.helper = helper;
        this.helper.Events.GameLoop.ReturnedToTitle += OnReturnToTitle;
    }

    private void OnReturnToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        CurrentRemoteView = null;
    }

    internal void BeginRemoteView(SObject farmComputer, Farmer player)
    {
        if (CurrentRemoteView != null)
        {
            return;
        }
        CurrentRemoteView = new(farmComputer, player, Game1.currentLocation, Game1.viewport.Location);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        // update remote view
        if (CurrentRemoteView?.Update(Game1.currentGameTime) ?? false)
        {
            CurrentRemoteView = null;
        }
    }
}
