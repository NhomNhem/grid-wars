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
    [Signal] public delegate void GridStateUpdatedEventHandler();
    
    private readonly HashSet<Vector2I> _validBuildableTiles = new();
    private readonly HashSet<Vector2I> _collectResourceTiles = new();
    private readonly HashSet<Vector2I> _occupiedTiles = new();
    
    private List<TileMapLayer> _allTileMapLayers = new();

    public override void _Ready() {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
        GameEvents.Instance.BuildingDestroyed += OnBuildingDestroyed;
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
        var expandedTiles = validTiles.Except(_validBuildableTiles).Except(_occupiedTiles);
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
        return ConvertWorldPositionToTilePositon(mouse);
    }

    public Vector2I ConvertWorldPositionToTilePositon(Vector2 worldPosition) {
        var mousePosition = worldPosition / 64;
        mousePosition = mousePosition.Floor();
        return new Vector2I((int)mousePosition.X, (int)mousePosition.Y);
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
        _occupiedTiles.Add(buildingComponent.GetGridCellPosition());
        var rootCell = buildingComponent.GetGridCellPosition();

        var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildingResource.BuildableRadius);
        _validBuildableTiles.UnionWith(validTiles);
        _validBuildableTiles.ExceptWith(_occupiedTiles);
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent) {
        var rootCell = buildingComponent.GetGridCellPosition();
        var resourceTiles = GetResourceTileInRadius(rootCell, buildingComponent.BuildingResource.ResourceRadius);
        
        var oldResourceCount = _collectResourceTiles.Count;
        _collectResourceTiles.UnionWith(resourceTiles);
        
        if (oldResourceCount != _collectResourceTiles.Count)
            EmitSignal(SignalName.ResourceTilesUpdated, _collectResourceTiles.Count);
        
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void ReCalculateGrid(BuildingComponent excludedBuildingComponent) {
        _occupiedTiles.Clear();
        _validBuildableTiles.Clear();
        _collectResourceTiles.Clear();
        var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>()
            .Where(b => b != excludedBuildingComponent);
        
        foreach (var buildingComponent in buildingComponents) {
            UpdateValidBuildableTiles(buildingComponent);
            UpdateCollectedResourceTiles(buildingComponent);
        }
        
        EmitSignal(SignalName.ResourceTilesUpdated, _collectResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);
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

    private void OnBuildingPlaced(BuildingComponent buildingComponent) {
        UpdateValidBuildableTiles(buildingComponent);
        UpdateCollectedResourceTiles(buildingComponent);
    }

    private void OnBuildingDestroyed(BuildingComponent buildingComponent) {
        ReCalculateGrid(buildingComponent);
    }
}

