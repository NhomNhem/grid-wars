using Game.resources.building;
using Godot;

namespace Game.ui;

public partial class GameUI : MarginContainer {
   

    [Export] private BuildingResource[] _buildingResources;
    
    [Signal] public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);
    
    private HBoxContainer _hBoxContainer;

    public override void _Ready() {
       _hBoxContainer = GetNode<HBoxContainer>("HBoxContainer");
       CreateBuildingButtons();
       
       
    }
    
    private void CreateBuildingButtons() {
        foreach (var buildingResource in _buildingResources) {
            var buildingButton = new Button();
            buildingButton.Text = $"Place {buildingResource.DisplayName}";
            _hBoxContainer.AddChild(buildingButton);
            
            buildingButton.Pressed += () => {
                EmitSignal(SignalName.BuildingResourceSelected, buildingResource);
            };
        }
    }
    
    
}