using Game.Autoload;
using Game.components;
using Godot;

namespace Game.manager;

public partial class GridManager : Node {
    private const string IS_BUILDABLE = "is_buildable";
    private const string IS_WOODED = "is_wood";

    [Export] private TileMapLayer _highLightTileMapLayer;
    [Export] private TileMapLayer _baseTerrainTileMapLayer;

    [Signal] public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);
    
    private readonly HashSet<Vector2I> _validBuildableTiles = new();
    private readonly HashSet<Vector2I> _collectResourceTiles = new();
    
    private List<TileMapLayer> _allTileMapLayers = new();

    public override void _Ready() {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
        _allTileMapLayers = GetAllTileMapLayers(_baseTerrainTileMapLayer);
    }

    private bool TileHasCustomData(Vector2I tilePos, string customDataName) {
        foreach (var layer in _allTileMapLayers) {
            var customData = layer.GetCellTileData(tilePos);
            if (customData == null) continue;
            return (bool)customData.GetCustomData(customDataName);
        }
        return false;
    }

    public void HighlightBuildableTiles() {
        foreach (var tilePositon in _validBuildableTiles)
            _highLightTileMapLayer.SetCell(tilePositon, 0, Vector2I.Zero);
    }

    public void HighLightExpandedBuildableTiles(Vector2I rootCell, int radius) {
        var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
        var expandedTiles = validTiles.Except(_validBuildableTiles).Except(GetOccupiedTiles());
        var atlasCoords = new Vector2I(1, 0);

        foreach (var tilePos in expandedTiles)
            _highLightTileMapLayer.SetCell(tilePos, 0, atlasCoords);
    }

    public void HighLightResourceTiles(Vector2I rootCell, int radius) {
        var resourceTiles = GetResourceTileInRadius(rootCell, radius);
        var atlasCoords = new Vector2I(1, 0);
        foreach (var tilePos in resourceTiles) {
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

    private List<TileMapLayer> GetAllTileMapLayers(TileMapLayer rootTileMapLayer) {
        var result = new List<TileMapLayer>();
        var children = rootTileMapLayer.GetChildren();
        children.Reverse();
        foreach (var child in children)
            if (child is TileMapLayer childLayer) {
                result.AddRange(GetAllTileMapLayers(childLayer));
            }

        result.Add(rootTileMapLayer);
        return result;
    }

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent) {
        var rootCell = buildingComponent.GetGridCellPosition();

        var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildingResource.BuildableRadius);
        _validBuildableTiles.UnionWith(validTiles);
        _validBuildableTiles.ExceptWith(GetOccupiedTiles());
    }

    private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent) {
        var rootCell = buildingComponent.GetGridCellPosition();
        var resourceTiles = GetResourceTileInRadius(rootCell, buildingComponent.BuildingResource.ResourceRadius);
        
        var oldResourceCount = _collectResourceTiles.Count;
        _collectResourceTiles.UnionWith(resourceTiles);
        
        if (oldResourceCount != _collectResourceTiles.Count)
            EmitSignal(SignalName.ResourceTilesUpdated, _collectResourceTiles.Count);
    }

    private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius, Func<Vector2I, bool> filterFn) {
        var result = new List<Vector2I>();

        for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
        for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++) {
            var tilePos = new Vector2I(x, y);
            if (!filterFn(tilePos)) continue;
            result.Add(tilePos);
        }

        return result;
    }
    
    private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius) 
        => GetTilesInRadius(rootCell, radius, (tilePos) 
            => TileHasCustomData(tilePos, IS_BUILDABLE));

    private List<Vector2I> GetResourceTileInRadius(Vector2I rootCell, int radius) 
        => GetTilesInRadius(rootCell, radius, (tilePos)
            => TileHasCustomData(tilePos, IS_WOODED));

    private IEnumerable<Vector2I> GetOccupiedTiles() {
        var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();
        var occupiedTiles = buildingComponents.Select(b => b.GetGridCellPosition());
        return occupiedTiles;
    }

    private void OnBuildingPlaced(BuildingComponent buildingComponent) {
        UpdateValidBuildableTiles(buildingComponent);
        UpdateCollectedResourceTiles(buildingComponent);
    }
}

// http://localhost:11434/v1