using Godot;
using Godot.Collections;

namespace GodotSTG;

[GlobalClass]
public partial class STGBulletModifier:Resource{

    [Export] public int bullet_id {get; set;} = 0;
    [Export] public Color outer_color {get; set;} = Colors.Red;
    [Export] public Color inner_color {get; set;} = Colors.White;
    public Color custom_data {get; set;}
    [Export] public float speed {get; set;}
    [Export] public double lifespan {get; set;} = 0;
    [Export] public Array<STGTween> tweens = new();
    [Export] public STGBulletModifier next {get; set;}

    // this are automatically set at runtime. dw about it.
    public int id = -1;
}