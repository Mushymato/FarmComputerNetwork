using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace FarmComputerNetwork;

public sealed class FarmComputerInfoBox : IClickableMenu
{
    private readonly GameLocation location;
    private readonly string displayName;
    private readonly string? infoTextCol1 = null;
    private readonly string? infoTextCol2 = null;

    public FarmComputerInfoBox(SObject farmComputer)
        : base(0, 0, 400, 400, false)
    {
        location = farmComputer.Location;
        displayName = farmComputer.Location.DisplayName;

        #region build info text
        StringBuilder sb1 = new();
        StringBuilder sb2 = new();

        sb1.Append(Game1.content.LoadString($"{AssetManager.ModStrings}:info.is-farm")).Append('\n');
        sb2.Append(
                location.IsFarm
                    ? Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")
                    : Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")
            )
            .Append('\n');

        if (location.piecesOfHay.Value > 0)
        {
            sb1.Append(
                    Game1
                        .content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_PiecesHay")
                        .Replace("{0}/{1}", "")
                )
                .Append('\n');
            sb2.Append(location.piecesOfHay.Value).Append('/').Append(location.GetHayCapacity()).Append('\n');
        }

        sb1.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_TotalCrops", "")).Append('\n');
        sb2.Append(location.getTotalCrops()).Append('\n');

        sb1.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_CropsReadyForHarvest", ""))
            .Append('\n');
        sb2.Append(location.getTotalCropsReadyForHarvest()).Append('\n');

        sb1.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_CropsUnwatered", ""))
            .Append('\n');
        sb2.Append(location.getTotalUnwateredCrops()).Append('\n');

        sb1.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_TotalOpenHoeDirt", ""))
            .Append('\n');
        sb2.Append(location.getTotalOpenHoeDirt()).Append('\n');

        sb1.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_TotalForage", "")).Append('\n');
        sb2.Append(location.getTotalForageItems()).Append('\n');

        sb1.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_MachinesReady", "")).Append('\n');
        sb2.Append(location.getNumberOfMachinesReadyForHarvest()).Append('\n');

        sb1.Append(Game1.content.LoadString($"{AssetManager.ModStrings}:info.fruit-trees-ready")).Append('\n');
        sb2.Append(GetFruitTreesReadyForHarvest(location)).Append('\n');

        sb1.Append(Game1.content.LoadString($"{AssetManager.ModStrings}:info.bushes-ready"));
        sb2.Append(GetBushesReadyForHarvest(location));

        infoTextCol1 = sb1.ToString();
        infoTextCol2 = sb2.ToString();
        #endregion
    }

    private static int GetFruitTreesReadyForHarvest(GameLocation location)
    {
        int ready = 0;
        foreach (TerrainFeature feature in location.terrainFeatures.Values)
        {
            if (feature is FruitTree fruitTree && fruitTree.fruit.Count > 0)
            {
                ready++;
            }
        }
        return ready;
    }

    private static bool InBloomBBM(Bush bush) => bush.modData.ContainsKey("NCarigon.BushBloomMod/bush-schedule");

    private static int GetBushesReadyForHarvest(GameLocation location)
    {
        int ready = 0;
        foreach (TerrainFeature feature in location.terrainFeatures.Values)
        {
            if (
                feature is Bush bush
                && bush.size.Value != 4
                && bush.inBloom()
                && (bush.readyForHarvest() || InBloomBBM(bush))
            )
            {
                ready++;
            }
        }
        return ready;
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);
        if (displayName == null || infoTextCol1 == null || infoTextCol2 == null)
        {
            return;
        }

        Vector2 titleSize = Game1.dialogueFont.MeasureString(displayName);
        Vector2 col1Size = Game1.dialogueFont.MeasureString(infoTextCol1);
        Vector2 col2Size = Game1.dialogueFont.MeasureString(infoTextCol2);

        int yOffset = 48;
        Utility.DrawSquare(
            b,
            new Rectangle(64, yOffset, (int)(titleSize.X + 32), (int)(titleSize.Y + 8)),
            4,
            borderColor: Color.Wheat * 0.5f,
            backgroundColor: Color.Black * 0.5f
        );
        b.DrawString(Game1.dialogueFont, displayName, new Vector2(64 + 16, yOffset + 6), Color.White);
        yOffset += (int)titleSize.Y + 8;
        yOffset += 16;

        Utility.DrawSquare(
            b,
            new Rectangle(64, yOffset, (int)(col1Size.X + 32), (int)(col1Size.Y + 8)),
            4,
            borderColor: Color.Wheat * 0.5f,
            backgroundColor: Color.Black * 0.5f
        );
        b.DrawString(Game1.dialogueFont, infoTextCol1, new Vector2(64 + 16, yOffset + 6), Color.White);

        Utility.DrawSquare(
            b,
            new Rectangle((int)(64 + 40 + col1Size.X), yOffset, (int)(col2Size.X + 32), (int)(col2Size.Y + 8)),
            4,
            borderColor: Color.Wheat * 0.5f,
            backgroundColor: Color.Black * 0.5f
        );
        b.DrawString(
            Game1.dialogueFont,
            infoTextCol2,
            new Vector2(64 + 40 + 16 + col1Size.X, yOffset + 6),
            Color.White
        );
    }
}
