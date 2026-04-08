using Godot;

namespace Game.manager;

public partial class GridManager : Node {
    [Export] private TileMapLayer _highLightTileMapLayer;
    [Export] private TileMapLayer _baseTerrainTileMapLayer;
    
    private HashSet<Vector2> _occupiedCells = new();

    public bool IsTileOccupied(Vector2I tilePos) {
        var customData = _baseTerrainTileMapLayer.GetCellTileData(tilePos);
        if (customData == null) return false; // treat out of bounds as unoccupied
        if (!(bool)customData.GetCustomData("buildable")) return false;
        return !_occupiedCells.Contains(tilePos);
    }

    public void MarkTileAsOccupied(Vector2I tilePos) => _occupiedCells.Add(tilePos);
    
    public void HighLightValidTilesInRadius(Vector2I rootCell, int radius) {
        ClearHighLightTiles();
        
        for (var x = rootCell.X - 3; x <= rootCell.X + 3; x++)
            for (var y = rootCell.Y - 3; y <= rootCell.Y + 3; y++) {
                var tilePos = new Vector2I(x, y);
                if (!IsTileOccupied(tilePos)) continue;
                _highLightTileMapLayer.SetCell(tilePos, 0, Vector2I.Zero);
            }
    }
    
    public void ClearHighLightTiles() => _highLightTileMapLayer.Clear();

    public Vector2I GetMouseGridCellPosition() {
        var mouse = _highLightTileMapLayer.GetGlobalMousePosition();
        var gridPosition = mouse / 64;
        gridPosition = gridPosition.Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }
    
}