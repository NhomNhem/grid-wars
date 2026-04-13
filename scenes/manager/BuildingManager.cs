using Game.resources.building;
using Game.ui;
using Godot;

namespace Game.manager;
public partial class BuildingManager : Node {

    [Export] private GridManager _gridManager;
    [Export] private GameUI _gameUI;
    [Export] private PackedScene _buildingGhostScene;
    [Export] private Node2D _ySortRoot;
    
    private Vector2I? _hoverGridCell;
    private int _currentResourceTilesCount;
    private int _currentlyUsedResourceTilesCount;
    private int _startingResourceTilesCount = 4;
    private int AvailableResourceCount => _startingResourceTilesCount + _currentResourceTilesCount - _currentlyUsedResourceTilesCount;
    
    private Node2D _buildingGhost;
    
    private BuildingResource _toPlaceBuildingResource;
    
    public override void _Ready() {
        _gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
        _gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
    }

    public override void _Process(double delta) {
        if (!IsInstanceValid(_buildingGhost)) return;
        
        var gridPosition = _gridManager.GetMouseGridCellPosition();
        _buildingGhost.GlobalPosition = gridPosition * 64;

        if (_toPlaceBuildingResource != null && (!_hoverGridCell.HasValue || _hoverGridCell.Value != gridPosition)) {
            _hoverGridCell = gridPosition;
            _gridManager.ClearHighlightedTiles();
            _gridManager.HighLightExpandedBuildableTiles(_hoverGridCell.Value, _toPlaceBuildingResource.BuildableRadius); 
            _gridManager.HighLightResourceTiles(_hoverGridCell.Value, _toPlaceBuildingResource.ResourceRadius);
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (_hoverGridCell.HasValue && 
            _toPlaceBuildingResource != null &&
            @event.IsActionPressed("left_click") &&
            AvailableResourceCount >= _toPlaceBuildingResource.ResourceCost &&
            _gridManager.IsTilePositionBuildable(_hoverGridCell.Value)) {
            PlaceBuildingAtHoveredCellPosition();
        }
    }

    private void PlaceBuildingAtHoveredCellPosition() {
        if (!_hoverGridCell.HasValue) return;

        var building = _toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        _ySortRoot.AddChild(building);

        building.GlobalPosition = _hoverGridCell.Value * 64;

        _hoverGridCell = null;
        _gridManager.ClearHighlightedTiles();

        _currentlyUsedResourceTilesCount += _toPlaceBuildingResource.ResourceCost;
        
        // Clear the ghost and exit placement mode
        if (IsInstanceValid(_buildingGhost)) {
            _buildingGhost.QueueFree();
            _buildingGhost = null;
        }
        _toPlaceBuildingResource = null;
    }
    
    private void OnResourceTilesUpdated(int resourceCount) {
        _currentResourceTilesCount = resourceCount;
    }
    
    private void OnBuildingResourceSelected(BuildingResource buildingResource) {
        if (IsInstanceValid(_buildingGhost)) _buildingGhost.QueueFree();
        
        _buildingGhost = _buildingGhostScene.Instantiate<Node2D>();
        _ySortRoot.AddChild(_buildingGhost);

        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        _buildingGhost.AddChild(buildingSprite);
        
        _toPlaceBuildingResource = buildingResource;
        _gridManager.HighlightBuildableTiles();
    }
}