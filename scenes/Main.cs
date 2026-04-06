using Godot;

namespace Game;

public partial class Main : Node {
    private Sprite2D _cursor;
    private PackedScene _buildingScene;
    private Button _placeBuildingButton;
    private TileMapLayer _highTileMapLayer;

    private Vector2? _hoverGridCell;
    private HashSet<Vector2> _occupiedCell = new();

    public override void _Ready() {
        _buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");
        _cursor = GetNode<Sprite2D>("Cursor");
        _placeBuildingButton = GetNode<Button>("PlaceBuildingButton");
        _highTileMapLayer = GetNode<TileMapLayer>("HighlightTileMapLayer");

        _cursor.Visible = false;

        _placeBuildingButton.Pressed += OnButtonPressed;
    }

    public override void _Process(double delta) {
        var gridPosition = GetMouseGridCellPosition();
        _cursor.GlobalPosition = gridPosition * 64;

        if (_cursor.Visible && (!_hoverGridCell.HasValue || _hoverGridCell.Value != gridPosition)) {
            _hoverGridCell = gridPosition;
            UpdateHighlightTileMapLayer();
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (_hoverGridCell.HasValue && _cursor.Visible && @event.IsActionPressed("left_click") &&
            !_occupiedCell.Contains(_hoverGridCell.Value)) {
            PlaceBuildingAtHoveredCellPosition();
            _cursor.Visible = false;
        }
    }

    private void PlaceBuildingAtHoveredCellPosition() {
        if (!_hoverGridCell.HasValue) return;
        
        var building = _buildingScene.Instantiate<Node2D>();
        AddChild(building);

        building.GlobalPosition = _hoverGridCell.Value * 64;
        _occupiedCell.Add(_hoverGridCell.Value);

        // update tilemap layer to remove highlight
        _hoverGridCell = null;
        UpdateHighlightTileMapLayer();
    }

    private Vector2 GetMouseGridCellPosition() {
        var mousePos = _highTileMapLayer.GetGlobalMousePosition();
        var gridPos = mousePos / 64;

        gridPos = gridPos.Floor();
        return gridPos;
    }

    private void UpdateHighlightTileMapLayer() {
        _highTileMapLayer.Clear();

        if (!_hoverGridCell.HasValue)
            return;

        for (var x = _hoverGridCell.Value.X - 3; x <= _hoverGridCell.Value.X + 3; x++)
        for (var y = _hoverGridCell.Value.Y - 3; y <= _hoverGridCell.Value.Y + 3; y++)
            _highTileMapLayer.SetCell(new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
    }

    private void OnButtonPressed() => _cursor.Visible = !_cursor.Visible;
}