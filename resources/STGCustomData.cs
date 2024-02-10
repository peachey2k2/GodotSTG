using Godot;

namespace GodotSTG;

[GlobalClass]
public partial class STGCustomData:Resource{
    [Export] public string name {get; set;}
    [Export] public int health {get; set;}
}