using Godot;
using Godot.Collections;
using GodotSTG;

namespace GodotSTG;

[GlobalClass, Icon("res://addons/GodotSTG/assets/sequence.png")]
public partial class STGSequence:Node{
    
    // [Export] public float wait_before {get; set;} = 1;
    [Export] public int end_at_hp {get; set;} = -1;
    [Export] public int end_at_time {get; set;} = -1;
    [Export] public bool persist {get; set;} = false;
    public async void spawn_sequence(){
        foreach (Node child in GetChildren()){
            if (child is STGSpawner spawner){
                spawner.spawn();
            }
        }
        await ToSignal(STGGlobal.Instance, STGGlobal.SignalName.spawner_done);
        STGGlobal.Instance.EmitSignal(STGGlobal.SignalName.end_sequence);
    }

}
