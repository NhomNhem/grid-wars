using Game.Autoload;
using Game.resources.building;
using Godot;

namespace Game.components;

public partial class BuildingComponent : Node2D {
    
    [Export(PropertyHint.File,"*.tres")] public string BuildingResourcePath;

    public BuildingResource BuildingResource { get; private set;}

    public override void _Ready() {
        if (BuildingResourcePath != null) 
            BuildingResource = GD.Load<BuildingResource>(BuildingResourcePath);
        AddToGroup(nameof(BuildingComponent));
        Callable.From(() => GameEvents.EmitBuildingPlaced(this)).CallDeferred();
    }

    public Vector2I GetGridCellPosition() {
        var gridPosition = GlobalPosition / 64;
        gridPosition = gridPosition.Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }

    public List<Vector2I> GetOccupiedCellPosition() {
        var result = new List<Vector2I>();
        var gridPosition = GetGridCellPosition();

        for (int x = gridPosition.X; x < gridPosition.X + BuildingResource.Dimensions.X; x++) {
            for (int Y = gridPosition.Y; Y < gridPosition.Y + BuildingResource.Dimensions.Y; Y++) {
                result.Add(new Vector2I(x, Y));
            }
        }
        
        return result;
    }
    
    public void Destroy() {
        GameEvents.EmitBuildingDestroyed(this);
        Owner.QueueFree(); // Owner is the BuildingComponents parent node, which is the actual building in the scene. We want to free that when we destroy the building.
    }
}