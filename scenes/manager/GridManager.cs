using Game.Autoload;
using Game.components;
using Godot;

namespace Game.manager;

public partial class GridManager : Node {
    [Export] private TileMapLayer _highLightTileMapLayer;
    [Export] private TileMapLayer _baseTerrainTileMapLayer;
    
    private readonly HashSet<Vector2I> _validBuildableTiles = new();

    public override void _Ready() {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
    }

    private bool IsTilePositionValid(Vector2I tilePos) {
        var customData = _baseTerrainTileMapLayer.GetCellTileData(tilePos);
        if (customData == null) return false; 
        return (bool)customData.GetCustomData("buildable");
    }
    
    public void HighlightBuildableTiles() {
        foreach (var tilePositon in _validBuildableTiles) {
            _highLightTileMapLayer.SetCell(tilePositon, 0, Vector2I.Zero);
        }
    }

    public void HighLightExpandedBuildableTiles(Vector2I rootCell, int radius) {
        ClearHighlightedTiles();
        HighlightBuildableTiles();
        
        var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
        var expandedTiles = validTiles.Except(_validBuildableTiles).Except(GetOccupiedTiles());
        var atlasCoords = new Vector2I(1, 0);
        
        foreach (var tilePos in expandedTiles) {
            _highLightTileMapLayer.SetCell(tilePos, 0, atlasCoords);
        }
    }
    
    public void ClearHighlightedTiles() => _highLightTileMapLayer.Clear();

    public Vector2I GetMouseGridCellPosition() {
        var mouse = _highLightTileMapLayer.GetGlobalMousePosition();
        var gridPosition = mouse / 64;
        gridPosition = gridPosition.Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }

    public bool IsTilePositionBuildable(Vector2I tilePos) => _validBuildableTiles.Contains(tilePos);

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent) {
        var rootCell = buildingComponent.GetGridCellPosition();
       
        var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildableRadius);
        _validBuildableTiles.UnionWith(validTiles);
        _validBuildableTiles.ExceptWith(GetOccupiedTiles());
    }

    private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius) {
        var result = new List<Vector2I>();

        for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
        for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++) {
            var tilePos = new Vector2I(x, y);
            if (!IsTilePositionValid(tilePos)) continue;
            result.Add(tilePos);
        }
        
        return result;
    }

    private IEnumerable<Vector2I> GetOccupiedTiles() {
        var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();
        var occupiedTiles = buildingComponents.Select(b => b.GetGridCellPosition());
        return occupiedTiles;
    }
    
    private void OnBuildingPlaced(BuildingComponent buildingComponent) {
        UpdateValidBuildableTiles(buildingComponent);
    }
}