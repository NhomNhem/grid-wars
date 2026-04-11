using Game.manager;
using Game.resources.building;
using Godot;

namespace Game;

public partial class Main : Node {
    
    private GridManager _gridManager;
    
    private Sprite2D _cursor;
    private BuildingResource _towerResource;
    private BuildingResource _villageResource;
    private Button _placeTowerButton;
    private Button _placeVillageButton;
    private Node2D _ySortRoot;

    private BuildingResource _toPlaceBuildingResource;
    private Vector2I? _hoverGridCell;

    public override void _Ready() {
        _towerResource = GD.Load<BuildingResource>("res://resources/building/tower.tres");
        _villageResource = GD.Load<BuildingResource>("res://resources/building/village.tres");
        
        
        _gridManager = GetNode<GridManager>("GridManager");
        
        _cursor = GetNode<Sprite2D>("Cursor");
        _placeTowerButton = GetNode<Button>("PlaceTowerButton");
        _placeVillageButton = GetNode<Button>("PlaceVillageButton");
        _ySortRoot = GetNode<Node2D>("YSortRoot");
        
        _cursor.Visible = false;

        _placeTowerButton.Pressed += OnPlaceTowerButtonPressed;
        _placeVillageButton.Pressed += OnPlaceVillageButtonPressed;
    }

    public override void _Process(double delta) {
        var gridPosition = _gridManager.GetMouseGridCellPosition();
        _cursor.GlobalPosition = gridPosition * 64;

        if (_toPlaceBuildingResource != null && _cursor.Visible && (!_hoverGridCell.HasValue || _hoverGridCell.Value != gridPosition)) {
            _hoverGridCell = gridPosition;
            _gridManager.ClearHighlightedTiles();
            _gridManager.HighLightExpandedBuildableTiles(_hoverGridCell.Value, _toPlaceBuildingResource.BuildableRadius); 
            _gridManager.HighLightResourceTiles(_hoverGridCell.Value, _toPlaceBuildingResource.ResourceRadius);
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (_hoverGridCell.HasValue && _cursor.Visible && @event.IsActionPressed("left_click") &&
            _gridManager.IsTilePositionBuildable(_hoverGridCell.Value)) {
            PlaceBuildingAtHoveredCellPosition();
            _cursor.Visible = false;
        }
    }

    private void PlaceBuildingAtHoveredCellPosition() {
        if (!_hoverGridCell.HasValue) return;
        
        var building = _toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        _ySortRoot.AddChild(building);

        building.GlobalPosition = _hoverGridCell.Value * 64;
        
        _hoverGridCell = null;
        _gridManager.ClearHighlightedTiles();
    }


    private void OnPlaceTowerButtonPressed() {
        _toPlaceBuildingResource = _towerResource;
        
        _cursor.Visible = true;
        
        _gridManager.HighlightBuildableTiles();
    }

    private void OnPlaceVillageButtonPressed() {
        _toPlaceBuildingResource = _villageResource;
        
        _cursor.Visible = true;
        
        _gridManager.HighlightBuildableTiles();
    }
}