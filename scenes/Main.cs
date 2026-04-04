using Godot;

public partial class Main : Node2D {
    private Sprite2D _cursor;
    private PackedScene _buildingScene;
    private Button _placeBuildingButton;

    public override void _Ready() {
        _buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");
        _cursor = GetNode<Sprite2D>("Cursor");
        _placeBuildingButton = GetNode<Button>("PlaceBuildingButton");

        _cursor.Visible = false;

        _placeBuildingButton.Pressed += OnButtonPressed;
    }

    public override void _Process(double delta) {
        if (!_cursor.Visible) return;
        var gridPosition = GetMouseGridCellPosition();
        _cursor.GlobalPosition = gridPosition * 64;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (!_cursor.Visible) return;

        if (@event.IsActionPressed("left_click")) {
            PlaceBuildingAtMousePosition();
        } else if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right) {
            _cursor.Visible = false;
        }
    }

    private void PlaceBuildingAtMousePosition() {
        var building = _buildingScene.Instantiate<Node2D>();
        AddChild(building);

        var gridPos = GetMouseGridCellPosition();
        building.GlobalPosition = gridPos * 64;
    }

    private Vector2 GetMouseGridCellPosition() {
        var mousePos = GetGlobalMousePosition();
        var gridPos = mousePos / 64;
        gridPos = gridPos.Floor();
        return gridPos;
    }


    private void OnButtonPressed() => _cursor.Visible = !_cursor.Visible;
}