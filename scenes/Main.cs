using Game.manager;
using Game.resources.building;
using Game.ui;
using Godot;

namespace Game;

public partial class Main : Node {

    public override void _Ready() {
       
    }
    
    private void OnResourceTilesUpdated(int resourceTilesCount) {
        GD.Print($"Resource tiles count: {resourceTilesCount}");
    }
}