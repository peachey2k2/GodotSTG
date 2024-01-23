using Godot;

namespace GodotSTG;

[GlobalClass]
public partial class STGCustomData:Resource{
    [Export] public string name {get; set;}
    [Export] public Color bar_color {get; set;} = Colors.White; // WHY IS IT COLORS AND NOT COLOR???
    [Export] bool shield {get; set;}
}