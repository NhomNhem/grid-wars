using Game.resources.building;
using Godot;

namespace Game.ui;

public partial class BuildingSection : PanelContainer {
    [Signal] public delegate void SelectButtonPressedEventHandler();
    
    private Label _titleLabel;
    private Label _descriptionLabel;
    private Label _costLabel;
    private Button _selectButton;

    public override void _Ready() {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _descriptionLabel = GetNode<Label>("%DescriptionLabel");
        _costLabel = GetNode<Label>("%CostLabel");
        _selectButton = GetNode<Button>("%Button");
        
        _selectButton.Pressed += () => EmitSignal(SignalName.SelectButtonPressed);
    }
    
    public void SetBuildingResource(BuildingResource buildingResource) {
        _titleLabel.Text = buildingResource.DisplayName;
        _descriptionLabel.Text = buildingResource.Description;
        _costLabel.Text = $"{buildingResource.ResourceCost}";
    }
}