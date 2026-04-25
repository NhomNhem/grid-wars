using Game.building;
using Game.components;
using Game.resources.building;
using Game.ui;
using Godot;

namespace Game.manager;

public partial class BuildingManager : Node {
    private readonly StringName ACTION_LEFT_CLICK = "left_click";
    private readonly StringName ACTION_CANCELED = "cancle";
    private readonly StringName ACTION_RIGHT_CLICK = "right_click";
    
    [Signal] public delegate void AvailableResourceCountChangedEventHandler(int availableResourceCount);
    
    private enum State {
        Normal,
        PlacingBuilding
    }
    
    [Export] private GridManager _gridManager;
    [Export] private GameUI _gameUI;
    [Export] private PackedScene _buildingGhostScene;
    [Export] private Node2D _ySortRoot;
    [Export] private int _startingResourceTilesCount = 4;

    private Rect2I _hoverGridArea = new(Vector2I.Zero, Vector2I.One);
    private int _currentResourceTilesCount;
    private int _currentlyUsedResourceTilesCount;
    
    private State _currentState = State.Normal;
    private BuildingGhost _buildingGhost;
    private BuildingResource _toPlaceBuildingResource;
    
    private int AvailableResourceCount => _startingResourceTilesCount + _currentResourceTilesCount - _currentlyUsedResourceTilesCount;

    public override void _Ready() {
        _gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
        _gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
        
        
        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
        
    }

    public override void _Process(double delta) {
        
        var mouseGridCellPosition = _gridManager.GetMouseGridCellPosition();
        var rootCell = _hoverGridArea.Position;
        if (rootCell != mouseGridCellPosition) {
            _hoverGridArea.Position = mouseGridCellPosition;
            UpdateHoveredGridArea();
        }

        switch (_currentState) {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                _buildingGhost.GlobalPosition = mouseGridCellPosition * 64;
                break;
        }
    }

    private void UpdateGridDisplay() {
        _gridManager.ClearHighlightedTiles();
        _gridManager.HighlightBuildableTiles();
        
        if (IsBuildingPlaceableAtArea(_hoverGridArea)) {
            _gridManager.HighLightExpandedBuildableTiles(_hoverGridArea, _toPlaceBuildingResource.BuildableRadius);
            _gridManager.HighLightResourceTiles(_hoverGridArea, _toPlaceBuildingResource.ResourceRadius);
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
                         IsBuildingPlaceableAtArea(_hoverGridArea)) {
                    PlaceBuildingAtHoveredCellPosition();
                }
                break;
        }
    }

    private void PlaceBuildingAtHoveredCellPosition() {
        var building = _toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        _ySortRoot.AddChild(building);

        building.GlobalPosition = _hoverGridArea.Position * 64;

        _currentlyUsedResourceTilesCount += _toPlaceBuildingResource.ResourceCost;

        ChangeState(State.Normal);
        
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void DestroyBuildingAtHoveredCellPosition() {
        var rootCell = _hoverGridArea.Position;
        var buildingComponent = GetTree().GetNodesInGroup(nameof(BuildingComponent))
            .Cast<BuildingComponent>()
            .FirstOrDefault(buildingComponent => buildingComponent.IsTileInBuildingArea(rootCell)
                                                 && buildingComponent.BuildingResource.IsDeletable);
        if (buildingComponent == null) return;
        
        _currentlyUsedResourceTilesCount -= buildingComponent.BuildingResource.ResourceCost;
        
        buildingComponent.Destroy();
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }
    
    private void ClearBuildingGhost() {
        _gridManager.ClearHighlightedTiles();
        if (IsInstanceValid(_buildingGhost)) {
            _buildingGhost.QueueFree();
            _buildingGhost = null;
        }
    }
    private bool IsBuildingPlaceableAtArea(Rect2I tileArea) {
        var allTilesInBuildable = _gridManager.IsTileAreaBuildable(tileArea);
        return AvailableResourceCount >= _toPlaceBuildingResource.ResourceCost && allTilesInBuildable;
    }
    
    private void UpdateHoveredGridArea() {
        switch (_currentState) {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                UpdateGridDisplay();
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
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource) {
        ChangeState(State.PlacingBuilding);
        _hoverGridArea.Size = buildingResource.Dimensions;
        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        _buildingGhost.AddChild(buildingSprite);

        _toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}