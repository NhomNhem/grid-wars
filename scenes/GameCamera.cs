using Godot;

namespace Game;
public partial class GameCamera : Camera2D {
    private const float PAN_SPEED = 200f;
    private const int TILE_SIZE = 64;
    
    private readonly StringName PAN_LEFT = "pan_left";
    private readonly StringName PAN_RIGHT = "pan_right";
    private readonly StringName PAN_UP = "pan_up";
    private readonly StringName PAN_DOWN = "pan_down";
    public override void _Process(double delta) {
        GlobalPosition = GetScreenCenterPosition();
        var movementVector = Input.GetVector(PAN_LEFT, PAN_RIGHT, PAN_UP, PAN_DOWN);
        GlobalPosition += movementVector * (float)delta * PAN_SPEED;
    }
    
    public void SetBoundingRect(Rect2I boundingRect) {
        LimitLeft = boundingRect.Position.X * TILE_SIZE;
        LimitRight = boundingRect.End.X * TILE_SIZE;
        LimitTop = boundingRect.Position.Y * TILE_SIZE;
        LimitBottom = boundingRect.End.Y * TILE_SIZE;
    }
    
    public void CenterOnPosition(Vector2 position) => GlobalPosition = position;
}