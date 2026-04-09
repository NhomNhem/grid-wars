using Game.manager;
using Godot;

namespace Game;

public partial class Main : Node {
    
    private GridManager _gridManager;
    
    private Sprite2D _cursor;
    private PackedScene _buildingScene;
    private Button _placeBuildingButton;

    private Vector2I? _hoverGridCell;

    public override void _Ready() {
        _buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");
        _gridManager = GetNode<GridManager>("GridManager");
        
        _cursor = GetNode<Sprite2D>("Cursor");
        _placeBuildingButton = GetNode<Button>("PlaceBuildingButton");
        
        _cursor.Visible = false;

        _placeBuildingButton.Pressed += OnButtonPressed;
    }

    public override void _Process(double delta) {
        var gridPosition = _gridManager.GetMouseGridCellPosition();
        _cursor.GlobalPosition = gridPosition * 64;

        if (_cursor.Visible && (!_hoverGridCell.HasValue || _hoverGridCell.Value != gridPosition)) {
            _hoverGridCell = gridPosition;
            _gridManager.HighLightBuildableTiles();
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (_hoverGridCell.HasValue && _cursor.Visible && @event.IsActionPressed("left_click") &&
            _gridManager.IsTileOccupied(_hoverGridCell.Value)) {
            PlaceBuildingAtHoveredCellPosition();
            _cursor.Visible = false;
        }
    }

    private void PlaceBuildingAtHoveredCellPosition() {
        if (!_hoverGridCell.HasValue) return;
        
        var building = _buildingScene.Instantiate<Node2D>();
        AddChild(building);

        building.GlobalPosition = _hoverGridCell.Value * 64;
        _gridManager.MarkTileAsOccupied(_hoverGridCell.Value);
        
        // update tilemap layer to remove highlight
        _hoverGridCell = null;
        _gridManager.ClearHighLightTiles();
    }


    private void OnButtonPressed() => _cursor.Visible = !_cursor.Visible;
}