using Game.manager;
using Game.resources.building;
using Game.ui;
using Godot;

namespace Game.level;

public partial class BaseLevel : Node {
    
    private GridManager _gridManager;
    private GoldMine _goldMine;
    private GameCamera _gameCamera;
    private Node2D _baseBuilding;
    private TileMapLayer _baseTerrainTileMapLayer;
    public override void _Ready() {
        _gridManager = GetNode<GridManager>("GridManager");
        _goldMine = GetNode<GoldMine>("%GoldMine");
        _gameCamera = GetNode<GameCamera>("GameCamera");
        _baseTerrainTileMapLayer = GetNode<TileMapLayer>("%BaseTerrainTileMapLayer");
        _baseBuilding = GetNode<Node2D>("%Base");
        
        _gameCamera.SetBoundingRect(_baseTerrainTileMapLayer.GetUsedRect());
        _gameCamera.CenterOnPosition(_baseBuilding.GlobalPosition);
        
        _gridManager.GridStateUpdated += OnGridStateUpdated;
    }
    
    private void OnGridStateUpdated() {
       var goldMineTilePosition = _gridManager.ConvertWorldPositionToTilePositon(_goldMine.GlobalPosition);
       if (_gridManager.IsTilePositionBuildable(goldMineTilePosition)) 
           _goldMine.SetActive();
    }
    
    
}