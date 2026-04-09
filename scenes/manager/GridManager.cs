using Game.Autoload;
using Game.components;
using Godot;

namespace Game.manager;

public partial class GridManager : Node {
    [Export] private TileMapLayer _highLightTileMapLayer;
    [Export] private TileMapLayer _baseTerrainTileMapLayer;
    
    private readonly HashSet<Vector2> _occupiedCells = new();

    public override void _Ready() {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
    }
    
    public bool IsTileOccupied(Vector2I tilePos) {
        var customData = _baseTerrainTileMapLayer.GetCellTileData(tilePos);
        if (customData == null) return false; // treat out of bounds as unoccupied
        if (!(bool)customData.GetCustomData("buildable")) return false;
        return !_occupiedCells.Contains(tilePos);
    }

    public void MarkTileAsOccupied(Vector2I tilePos) => _occupiedCells.Add(tilePos);

    public void HighLightBuildableTiles() {
        ClearHighLightTiles();
        var buildingComponents =
            GetTree().GetNodesInGroup(nameof(components.BuildingComponent)).Cast<BuildingComponent>();
        foreach (var building in buildingComponents) {
            HighLightValidTilesInRadius(building.GetGridCellPosition(), building.BuildableRadius);
        }
    }
    
    public void ClearHighLightTiles() => _highLightTileMapLayer.Clear();

    public Vector2I GetMouseGridCellPosition() {
        var mouse = _highLightTileMapLayer.GetGlobalMousePosition();
        var gridPosition = mouse / 64;
        gridPosition = gridPosition.Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }
    
    private void HighLightValidTilesInRadius(Vector2I rootCell, int radius) {
        for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
        for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++) {
            var tilePos = new Vector2I(x, y);
            if (!IsTileOccupied(tilePos)) continue;
            _highLightTileMapLayer.SetCell(tilePos, 0, Vector2I.Zero);
        }
    }
    
    private void OnBuildingPlaced(BuildingComponent buildingComponent) {
        var gridCell = buildingComponent.GetGridCellPosition();
        MarkTileAsOccupied(gridCell);
        HighLightBuildableTiles();
    }
}