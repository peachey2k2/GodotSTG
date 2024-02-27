using System;
using Godot;
using Godot.Collections;
using GodotSTG;

namespace GodotSTG;

[GlobalClass, Icon("res://addons/GodotSTG/assets/spell.png")]
public partial class STGSpell:Node{
    public enum Movement{Static, Random}

    [Export] public STGCustomData custom_data {get; set;} = new();
    // [Export] public string name {get; set;}
    // [Export] public int health {get; set;}
    [Export] public int time {get; set;} = 30;
    // [Export] public Color bar_color {get; set;} = Colors.White; // WHY IS IT COLORS AND NOT COLOR???
    [Export] public Vector2 enemy_pos {get; set;} = new(0.5f, 0.5f);
    [Export] public Movement enemy_movement {get; set;}
    // [Export] public Shield shield {get; set;}
    [Export] public float wait_before {get; set;}
    [Export] public float wait_between {get; set;}
    [Export(PropertyHint.Flags, 
        "Randomize sequences:1,"+
        "Loop sequences:2,"+
        "Clear after each:4")]
    public int sequence_flags {get; set;}
}