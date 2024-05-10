using Godot;

namespace GodotSTG;

[GlobalClass, /*Icon("res://addons/GodotSTG/assets/wait.png")*/]
public partial class STGWait:Node{
    [Export (PropertyHint.Range, "0,100,0.01,or_greater")] public float time = 1.0f;   
}
