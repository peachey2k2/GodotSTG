using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using GodotSTG;

namespace GodotSTG;

[GlobalClass]
public partial class STGSpawner:Resource{
    
    public static STGGlobal STGGlobal;
    public static SceneTree tree;
    public enum PosType{Absolute, Relative}
    public enum Towards{Generic, Player}

    [ExportGroup("Spawner")]
    [Export] public Vector2 position {get; set;}
    [Export] public PosType position_type {get; set;}
    [Export] public float rotation_speed {get; set;}

    [ExportGroup("Bullet")]
    [Export] public STGBulletModifier bullet {get; set;}

    public Vector2 real_pos;
    public bool stop_flag;
    public bool is_running;

    public STGBulletData bdata;
    public Texture2D tex;

    public async void spawn(){
        STGGlobal = STGGlobal.Instance;
        tree = STGGlobal.GetTree();
        
        if (is_running) return;
        is_running = true;
        stop_flag = false;
        bdata = STGGlobal.bltdata[bullet.bullet_id];
        // tex = STGGlobal.textures[bullet.id];
        if (position_type == PosType.Relative){
            real_pos = STGGlobal.lerp4arena(position) + STGGlobal.controller.enemy.Position - STGGlobal.arena_rect.Position;
        } else {
            real_pos = STGGlobal.lerp4arena(position);
        }
        STGGlobal.stop_all_spawners += stop_spawner;
        await _spawn();
        STGGlobal.EmitSignal(STGGlobal.SignalName.spawner_done);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async virtual Task _spawn(){
        Debug.Assert(false, "No \"_spawn()\" found.");
        return;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public void stop_spawner(){
        stop_flag = true;
        is_running = false;
    }

    public void spawn_bullet(Vector2 pos, float dir, float mag){
        // STGBulletData _bdata = (STGBulletData)bdata.Duplicate();
        STGBulletInstance _bdata = new(bdata, bullet){
            current = 0,
            position = pos,
            direction = dir,
            magnitude = mag
        };
        STGGlobal.create_bullet(_bdata);
    }
}