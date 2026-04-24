using Game.Autoload;
using Game.components;
using Game.level.util;
using Godot;

namespace Game.manager;

public partial class GridManager : Node {
    private const string IS_BUILDABLE = "is_buildable";
    private const string IS_WOODED = "is_wood";
    private const string IS_IGNORE = "is_ignored";

    [Export] private TileMapLayer _highLightTileMapLayer;
    [Export] private TileMapLayer _baseTerrainTileMapLayer;

    [Signal] public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);
    [Signal] public delegate void GridStateUpdatedEventHandler();
    
    private readonly HashSet<Vector2I> _validBuildableTiles = new();
    private readonly HashSet<Vector2I> _collectResourceTiles = new();
    private readonly HashSet<Vector2I> _occupiedTiles = new();
    public readonly Dictionary<TileMapLayer, ElevationLayer> _tileMapLayerToElevationLayer = new();
    
    private List<TileMapLayer> _allTileMapLayers = new();

    public override void _Ready() {
        GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
        GameEvents.Instance.BuildingDestroyed += OnBuildingDestroyed;
        _allTileMapLayers = GetAllTileMapLayers(_baseTerrainTileMapLayer);
        MapTileMapLayersToElevationLayers();
    }

    private (TileMapLayer, bool) GetTileHasCustomData(Vector2I tilePos, string customDataName) {
        foreach (var layer in _allTileMapLayers) {
            var customData = layer.GetCellTileData(tilePos);
            if (customData == null || (bool)customData.GetCustomData(IS_IGNORE)) continue;
            return (layer, (bool)customData.GetCustomData(customDataName));
        }
        return (null,false);
    }

    public void HighlightBuildableTiles() {
        foreach (var tilePositon in _validBuildableTiles)
            _highLightTileMapLayer.SetCell(tilePositon, 0, Vector2I.Zero);
    }

    public void HighLightExpandedBuildableTiles(Rect2I tileArea, int radius) {
        var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();
        var expandedTiles = validTiles.Except(_validBuildableTiles).Except(_occupiedTiles);
        var atlasCoords = new Vector2I(1, 0);

        foreach (var tilePos in expandedTiles)
            _highLightTileMapLayer.SetCell(tilePos, 0, atlasCoords);
    }

    public void HighLightResourceTiles(Rect2I tileArea, int radius) {
        var resourceTiles = GetResourceTileInRadius(tileArea, radius);
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

    public bool IsTileAreaBuildable(Rect2I tileArea){
        var tiles = tileArea.ToTiles();
        if (tiles.Count == 0) return false;
        
        (TileMapLayer firstTileMapLayer, _) = GetTileHasCustomData(tiles[0], IS_BUILDABLE);
        var targetElevationLayer = _tileMapLayerToElevationLayer[firstTileMapLayer];
        
        return tiles.All(tilePosition => {
            (TileMapLayer tileMapLayerToElevationLayer, bool isBuildable ) = GetTileHasCustomData(tilePosition, IS_BUILDABLE);
            var elevationLayer = _tileMapLayerToElevationLayer[tileMapLayerToElevationLayer];
            return isBuildable && _validBuildableTiles.Contains(tilePosition) && elevationLayer == targetElevationLayer;
        });
    }

    private List<TileMapLayer> GetAllTileMapLayers(Node2D rootNode) {
        var result = new List<TileMapLayer>();
        var children = rootNode.GetChildren();
        children.Reverse();
        foreach (var child in children)
            if (child is Node2D childNode) {
                result.AddRange(GetAllTileMapLayers(childNode));
            }

        if (rootNode is TileMapLayer tileMapLayer)
            result.Add(tileMapLayer);
            
        return result;
    }

    private void MapTileMapLayersToElevationLayers() {
        foreach (var layer in _allTileMapLayers) {
            ElevationLayer elevationLayer;
            Node startNode = layer;
            do {
                var parent = startNode.GetParent();
                elevationLayer = parent as ElevationLayer;
                startNode = parent;
            }while(elevationLayer == null && startNode != null);
            
            _tileMapLayerToElevationLayer[layer] = elevationLayer;
        }
    }

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent) {
        _occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPosition());
        var rootCell = buildingComponent.GetGridCellPosition();
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
        _validBuildableTiles.UnionWith(validTiles);
        _validBuildableTiles.ExceptWith(_occupiedTiles);
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent) {
        var rootCell = buildingComponent.GetGridCellPosition();
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        var resourceTiles = GetResourceTileInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);
        
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

    private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius) {
        var distanceX = centerPosition.X - (tilePosition.X + 0.5f);
        var distanceY = centerPosition.Y - tilePosition.Y + 0.5f;
        var distanceSquared = distanceX * distanceX + distanceY * distanceY;
        return distanceSquared <= radius * radius;
    }
    
    private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn) {
        var result = new List<Vector2I>();
        var tileAreaF = tileArea.ToRect2F();
        var tileAreaCenter = tileAreaF.GetCenter();
        var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;
        for (var x = tileArea.Position.X - radius; x <= tileArea.End.X + radius; x++)
        for (var y = tileArea.Position.Y - radius; y <= tileArea.End.Y + radius; y++) {
            var tilePos = new Vector2I(x, y);
            if (!IsTileInsideCircle(tileAreaCenter, tilePos, radius + radiusMod) || !filterFn(tilePos)) continue;
            result.Add(tilePos);
        }

        return result;
    }
    
    private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius) 
        => GetTilesInRadius(tileArea, radius, (tilePos) 
            => GetTileHasCustomData(tilePos, IS_BUILDABLE).Item2);

    private List<Vector2I> GetResourceTileInRadius(Rect2I tileArea, int radius) 
        => GetTilesInRadius(tileArea, radius, (tilePos)
            => GetTileHasCustomData(tilePos, IS_WOODED).Item2);

    private void OnBuildingPlaced(BuildingComponent buildingComponent) {
        UpdateValidBuildableTiles(buildingComponent);
        UpdateCollectedResourceTiles(buildingComponent);
    }

    private void OnBuildingDestroyed(BuildingComponent buildingComponent) {
        ReCalculateGrid(buildingComponent);
    }
}

