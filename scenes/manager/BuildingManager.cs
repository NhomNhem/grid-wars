using Game.building;
using Game.resources.building;
using Game.ui;
using Godot;

namespace Game.manager;

public partial class BuildingManager : Node {
    private readonly StringName ACTION_LEFT_CLICK = "left_click";
    private readonly StringName ACTION_CANCELED = "cancle";
    private readonly StringName ACTION_RIGHT_CLICK = "right_click";
    
    private enum State {
        Normal,
        PlacingBuilding
    }
    
    [Export] private GridManager _gridManager;
    [Export] private GameUI _gameUI;
    [Export] private PackedScene _buildingGhostScene;
    [Export] private Node2D _ySortRoot;

    private Vector2I _hoverGridCell;
    private int _currentResourceTilesCount;
    private int _currentlyUsedResourceTilesCount;
    private int _startingResourceTilesCount = 4;
    
    private State _currentState = State.Normal;
    private BuildingGhost _buildingGhost;
    private BuildingResource _toPlaceBuildingResource;
    
    private int AvailableResourceCount => _startingResourceTilesCount + _currentResourceTilesCount - _currentlyUsedResourceTilesCount;

    public override void _Ready() {
        _gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
        _gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
    }

    public override void _Process(double delta) {
        
        var gridPosition = _gridManager.GetMouseGridCellPosition();
        if (_hoverGridCell != gridPosition) {
            _hoverGridCell = gridPosition;
            UpdateHoveredGridCell();
        }

        switch (_currentState) {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                _buildingGhost.GlobalPosition = gridPosition * 64;
                break;
        }
    }

    private void UpdateGridDisplay() {
        _gridManager.ClearHighlightedTiles();
        _gridManager.HighlightBuildableTiles();
        
        if (IsBuildingPlaceableAtTile(_hoverGridCell)) {
            _gridManager.HighLightExpandedBuildableTiles(_hoverGridCell, _toPlaceBuildingResource.BuildableRadius);
            _gridManager.HighLightResourceTiles(_hoverGridCell, _toPlaceBuildingResource.ResourceRadius);
            _buildingGhost.SetValid();
        }
        else {
            _buildingGhost.SetInValid();
        }
            
    }
    
    
    public override void _UnhandledInput(InputEvent @event) {

        switch (_currentState) {
            case State.Normal:
                if (@event.IsActionPressed(ACTION_RIGHT_CLICK)) {
                    DestroyBuildingAtHoveredCellPosition();
                }
                break;
            case State.PlacingBuilding:
                if (@event.IsActionPressed(ACTION_CANCELED))
                    ChangeState(State.Normal);
                else if (_toPlaceBuildingResource != null &&
                         @event.IsActionPressed(ACTION_LEFT_CLICK) && 
                         IsBuildingPlaceableAtTile(_hoverGridCell)) {
                    PlaceBuildingAtHoveredCellPosition();
                }
                break;
            default: 
                break;
        }
    }

    private void PlaceBuildingAtHoveredCellPosition() {
        var building = _toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        _ySortRoot.AddChild(building);

        building.GlobalPosition = _hoverGridCell * 64;

        _currentlyUsedResourceTilesCount += _toPlaceBuildingResource.ResourceCost;

        ChangeState(State.Normal);
    }

    private void DestroyBuildingAtHoveredCellPosition() {
        
    }
    
    private void ClearBuildingGhost() {
        _gridManager.ClearHighlightedTiles();
        if (IsInstanceValid(_buildingGhost)) {
            _buildingGhost.QueueFree();
            _buildingGhost = null;
        }
    }
    private bool IsBuildingPlaceableAtTile(Vector2I tilePos) {
        return AvailableResourceCount >= _toPlaceBuildingResource.ResourceCost &&
               _gridManager.IsTilePositionBuildable(tilePos);
    }

    private void UpdateHoveredGridCell() {
        switch (_currentState) {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                UpdateGridDisplay();
                break;
            default:
                break;
        }
    }

    private void ChangeState(State toState) {
        switch (_currentState) {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                ClearBuildingGhost();
                _toPlaceBuildingResource = null;
                break;
        }
        
        _currentState = toState;

        switch (_currentState) {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                _buildingGhost = _buildingGhostScene.Instantiate<BuildingGhost>();
                _ySortRoot.AddChild(_buildingGhost);
                break;
        }
    }
    
    private void OnResourceTilesUpdated(int resourceCount) {
        _currentResourceTilesCount = resourceCount;
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource) {
        ChangeState(State.PlacingBuilding);
        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        _buildingGhost.AddChild(buildingSprite);

        _toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}