namespace LSLib.VirtualTextures;

public class TileSetGeometryCalculator
{
    public List<BuildTexture> Textures;
    public TileSetBuildData BuildData;

    private int PlacementTileWidth = 0x1000;
    private int PlacementTileHeight = 0x1000;
    private int PlacementGridWidth;
    private int PlacementGridHeight;
    private BuildTexture[] PlacementGrid;

    private void ResizePlacementGrid(int w, int h)
    {
        PlacementGridWidth = w;
        PlacementGridHeight = h;
        PlacementGrid = new BuildTexture[w * h];
    }

    private void GrowPlacementGrid()
    {
        if (PlacementGridWidth * PlacementTileWidth <= PlacementGridHeight * PlacementTileHeight)
        {
            ResizePlacementGrid(PlacementGridWidth * 2, PlacementGridHeight);
        }
        else
        {
            ResizePlacementGrid(PlacementGridWidth, PlacementGridHeight * 2);
        }
    }

    private bool TryToPlaceTexture(BuildTexture texture, int texX, int texY)
    {
        var width = texture.Width / BuildData.RawTileWidth / PlacementTileWidth;
        var height = texture.Height / BuildData.RawTileHeight / PlacementTileHeight;

        for (var y = texY; y < texY + height; y++)
        {
            for (var x = texX; x < texX + width; x++)
            {
                if (PlacementGrid[x + y * PlacementGridWidth] != null)
                {
                    return false;
                }
            }
        }

        texture.X = texX * PlacementTileWidth * BuildData.RawTileWidth;
        texture.Y = texY * PlacementTileHeight * BuildData.RawTileHeight;

        for (var y = texY; y < texY + height; y++)
        {
            for (var x = texX; x < texX + width; x++)
            {
                PlacementGrid[x + y * PlacementGridWidth] = texture;
            }
        }

        return true;
    }

    private bool TryToPlaceTexture(BuildTexture texture)
    {
        var width = texture.Width / BuildData.RawTileWidth / PlacementTileWidth;
        var height = texture.Height / BuildData.RawTileHeight / PlacementTileHeight;

        for (var y = 0; y < PlacementGridHeight - height + 1; y++)
        {
            for (var x = 0; x < PlacementGridWidth - width + 1; x++)
            {
                if (TryToPlaceTexture(texture, x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool PlaceAllTextures()
    {
        foreach (var tex in Textures)
        {
            if (!TryToPlaceTexture(tex))
            {
                return false;
            }
        }

        return true;
    }

    private void DoAutoPlacement()
    {
        var startingX = 0;
        var startingY = 0;

        foreach (var tex in Textures)
        {
            PlacementTileWidth = Math.Min(PlacementTileWidth, tex.Width / BuildData.RawTileWidth);
            PlacementTileHeight = Math.Min(PlacementTileHeight, tex.Height / BuildData.RawTileHeight);
            startingX = Math.Max(startingX, tex.Width / BuildData.RawTileWidth);
            startingY = Math.Max(startingY, tex.Height / BuildData.RawTileHeight);
        }

        ResizePlacementGrid(startingX / PlacementTileWidth, startingY / PlacementTileHeight);

        while (!PlaceAllTextures())
        {
            GrowPlacementGrid();
        }

        BuildData.TotalWidth = PlacementTileWidth * PlacementGridWidth * BuildData.RawTileWidth;
        BuildData.TotalHeight = PlacementTileHeight * PlacementGridHeight * BuildData.RawTileWidth;
    }

    private void UpdateGeometry()
    {
        var minTexSize = 0x10000;
        foreach (var tex in Textures)
        {
            minTexSize = Math.Min(minTexSize, Math.Min(tex.Height / BuildData.RawTileHeight, tex.Width / BuildData.RawTileHeight));
        }

        BuildData.MipFileStartLevel = 0;
        while (minTexSize > 0)
        {
            BuildData.MipFileStartLevel++;
            minTexSize >>= 1;
        }

        // Min W/H of all textures
        var minSize = Math.Min(BuildData.TotalWidth / BuildData.RawTileHeight, BuildData.TotalHeight / BuildData.RawTileHeight);
        BuildData.PageFileLevels = 0;
        while (minSize > 0)
        {
            BuildData.PageFileLevels++;
            minSize >>= 1;
        }

        BuildData.BuildLevels = BuildData.PageFileLevels + 1;

        foreach (var layer in BuildData.Layers)
        {
            var levelWidth = BuildData.TotalWidth;
            var levelHeight = BuildData.TotalHeight;

            layer.Levels = new List<BuildLevel>(BuildData.BuildLevels);
            for (var i = 0; i < BuildData.BuildLevels; i++)
            {
                var tilesX = levelWidth / BuildData.RawTileWidth + (((levelWidth % BuildData.RawTileWidth) > 0) ? 1 : 0);
                var tilesY = levelHeight / BuildData.RawTileHeight + (((levelHeight % BuildData.RawTileHeight) > 0) ? 1 : 0);
                var level = new BuildLevel
                {
                    Level = i,
                    Width = tilesX * BuildData.RawTileWidth,
                    Height = tilesY * BuildData.RawTileHeight,
                    TilesX = tilesX,
                    TilesY = tilesY,
                    PaddedTileWidth = BuildData.PaddedTileWidth,
                    PaddedTileHeight = BuildData.PaddedTileHeight,
                    Tiles = new BuildTile[tilesX * tilesY]
                };
                layer.Levels.Add(level);

                levelWidth = Math.Max(1, levelWidth >> 1);
                levelHeight = Math.Max(1, levelHeight >> 1);
            }
        }
    }

    public void Update()
    {
        DoAutoPlacement();
        UpdateGeometry();
    }
}
