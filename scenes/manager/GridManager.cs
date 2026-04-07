using Godot;

namespace Game.manager;

public partial class GridManager : Node {
    [Export] private TileMapLayer _highLightTileMapLayer;
    [Export] private TileMapLayer _baseTerrainTileMapLayer;
    
    private HashSet<Vector2> _occupiedCells = new();
    
    public override void _Ready() {
        
    }

    public bool IsTileOccupied(Vector2 cell) => !_occupiedCells.Contains(cell);
    
    public void MarkTileAsOccupied(Vector2 cell) => _occupiedCells.Add(cell);
    
    public void HighLightValidTilesInRadius(Vector2 rootCell, int radius) {
        ClearHighLightTiles();
        
        for (var x = rootCell.X - 3; x <= rootCell.X + 3; x++)
            for (var y = rootCell.Y - 3; y <= rootCell.Y + 3; y++) {
                if (!IsTileOccupied(new Vector2(x, y))) continue;
                _highLightTileMapLayer.SetCell(new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
            }
    }
    
    public void ClearHighLightTiles() => _highLightTileMapLayer.Clear();

    public Vector2 GetMouseGridCellPosition() {
        var mouse = _highLightTileMapLayer.GetGlobalMousePosition();
        var gridPosition = mouse / 64;
        gridPosition = gridPosition.Floor();
        return gridPosition;
    }
    
}