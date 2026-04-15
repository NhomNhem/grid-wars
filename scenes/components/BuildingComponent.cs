using Game.Autoload;
using Game.resources.building;
using Godot;

namespace Game.components;

public partial class BuildingComponent : Node2D {
    
    [Export(PropertyHint.File,"*.tres")] private string BuildingResourcePath;

    public BuildingResource BuildingResource { get; private set;}

    private readonly HashSet<Vector2I> _occupiedTile = new();

    public override void _Ready() {
        if (BuildingResourcePath != null) 
            BuildingResource = GD.Load<BuildingResource>(BuildingResourcePath);
        AddToGroup(nameof(BuildingComponent));
        Callable.From(InitializeOccupiedTile).CallDeferred();
    }

    public Vector2I GetGridCellPosition() {
        var gridPosition = GlobalPosition / 64;
        gridPosition = gridPosition.Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }

    public HashSet<Vector2I> GetOccupiedCellPosition() => _occupiedTile.ToHashSet();
    
    public bool IsTileInBuildingArea(Vector2I tilePos) => _occupiedTile.Contains(tilePos);
    
    public void Destroy() {
        GameEvents.EmitBuildingDestroyed(this);
        Owner.QueueFree(); // Owner is the BuildingComponents parent node, which is the actual building in the scene. We want to free that when we destroy the building.
    }

    public void CalculateOccupiedCellPosition() {
        var gridPosition = GetGridCellPosition();

        for (int x = gridPosition.X; x < gridPosition.X + BuildingResource.Dimensions.X; x++)
        for (int Y = gridPosition.Y; Y < gridPosition.Y + BuildingResource.Dimensions.Y; Y++)
            _occupiedTile.Add(new Vector2I(x, Y));
    }
    
    private void InitializeOccupiedTile() {
        CalculateOccupiedCellPosition();
        GameEvents.EmitBuildingPlaced(this);
    }
}