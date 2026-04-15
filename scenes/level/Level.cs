using Game.manager;
using Game.resources.building;
using Game.ui;
using Godot;

namespace Game;

public partial class Level : Node {
    private GridManager _gridManager;
    private GoldMine _goldMine;
    public override void _Ready() {
        _gridManager = GetNode<GridManager>("GridManager");
        _goldMine = GetNode<GoldMine>("%GoldMine");
        
        _gridManager.GridStateUpdated += OnGridStateUpdated;
    }
    
    private void OnGridStateUpdated() {
       var goldMineTilePosition = _gridManager.ConvertWorldPositionToTilePositon(_goldMine.GlobalPosition);
       if (_gridManager.IsTilePositionBuildable(goldMineTilePosition)) 
           _goldMine.SetActive();
    }
}