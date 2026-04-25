using Game.manager;
using Game.resources.building;
using Godot;

namespace Game.ui;

public partial class GameUI : CanvasLayer {
   
    [Export] private BuildingManager _buildingManager;
    [Export] private BuildingResource[] _buildingResources;
    [Export] private PackedScene _buildingSectionScene;
    
    [Signal] public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);
    
    private Label _resourceCountLabel;
    private VBoxContainer _buildingSectionContainer;

    public override void _Ready() {
        _buildingSectionContainer = GetNode<VBoxContainer>("%BuildingSectionContainer");
        _resourceCountLabel = GetNode<Label>("%ResourceLabel");
       CreateBuildingSection();
       
       _buildingManager. AvailableResourceCountChanged += OnAvailableResourceCountChanged;
    }

    private void CreateBuildingSection() {
        foreach (var buildingResource in _buildingResources) {
            var buildingSection = _buildingSectionScene.Instantiate<BuildingSection>();
            _buildingSectionContainer.AddChild(buildingSection);
            buildingSection.SetBuildingResource(buildingResource);
            
            buildingSection.SelectButtonPressed += () => EmitSignal(SignalName.BuildingResourceSelected, buildingResource);
        }
    }

    private void OnAvailableResourceCountChanged(int availableResourceCount) {
        _resourceCountLabel.Text = availableResourceCount.ToString();
    }
}