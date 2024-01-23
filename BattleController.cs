using Godot;
using GodotSTG;
using Godot.Collections;
using System;
using System.Threading.Tasks;

[GlobalClass, Icon("res://addons/GodotSTG/assets/battlecontroller.png")]
public partial class BattleController:Node2D{
    private static STGGlobal STGGlobal;
    [ExportCategory("BattleController")]
    [Export] private STGStats stats {get; set;}

    public SceneTree tree;
    public Timer timer;
    public bool is_spell_over;
    public int flag;

    public int hp_threshold;
    public int time_threshold;

    [Export] public CollisionObject2D player;
    [Export] public CollisionObject2D enemy;
    [Export] Rect2 arena_rect;
    string signals_are_hard;

    public override void _Ready(){
        STGGlobal = STGGlobal.Instance;

        tree = GetTree();
        timer = new(){OneShot = true};
        STGGlobal.end_sequence += _on_end_sequence;
        timer.Timeout += _on_spell_timed_out;
        STGGlobal.bar_emptied += _on_bar_emptied;
        STGGlobal.damage_taken += _on_damage_taken;
        AddChild(timer);

        Material = new ShaderMaterial(){
            Shader = (Shader)ResourceLoader.Load("res://addons/GodotSTG/BulletModulate.gdshader")
        };
        
    }

    public async void start(){
        GodotSTG.Debug.Assert(player != null, "\"player\" has to be set in order for start() to work.");
        GodotSTG.Debug.Assert(enemy != null, "\"enemy\" has to be set in order for start() to work.");
        // GodotSTG.Debug.Assert(arena_rect != null, "\"arena_rect\" has to be set in order for start() to work.");
        STGGlobal.clear();
        STGGlobal.shared_area.Reparent(this, false);
        STGGlobal.controller = this;
        STGGlobal.arena_rect = arena_rect;
        STGGlobal.EmitSignal(STGGlobal.SignalName.battle_start);
        int bar_count = stats.bars.Count;
        STGGlobal.EmitSignal(STGGlobal.SignalName.bar_changed, bar_count);
        player.Position = STGGlobal.lerp4arena(stats.player_position);
        foreach (STGBar curr_bar in stats.bars){
            foreach (STGSpell curr_spell in curr_bar.spells){
                is_spell_over = false;
                enemy.Position = STGGlobal.lerp4arena(curr_spell.enemy_pos);
                timer.WaitTime = curr_spell.time;
                timer.Start();
                STGGlobal.EmitSignal(STGGlobal.SignalName.spell_changed, curr_spell.custom_data, curr_spell.health);
                flag++; // timer await is encapsulated in flag increments and decrements
                await ToSignal(GetTree().CreateTimer(curr_spell.wait_before, false), SceneTreeTimer.SignalName.Timeout);
                flag--; // to prevent running multiple instances at the same time
                if (flag > 0) return;
                while (!is_spell_over){
                    foreach (STGSequence curr_sequence in curr_spell.sequences){
                        if (is_spell_over) break;
                        hp_threshold = curr_sequence.end_at_hp;
                        time_threshold = curr_sequence.end_at_time;
                        curr_sequence.spawn_sequence();
                        flag++; // timer await is encapsulated in flag increments and decrements
                        await ToSignal(STGGlobal, STGGlobal.SignalName.end_sequence); //
                        if (is_spell_over){
                            flag--;
                            break;
                        }
                        await ToSignal(GetTree().CreateTimer(curr_spell.wait_between, false), SceneTreeTimer.SignalName.Timeout); //
                        flag--; // to prevent running multiple instances at the same time
                        if (flag > 0) return;
                        if ((curr_spell.sequence_flags&4) == 4) STGGlobal.clear();
                    }
                    if ((curr_spell.sequence_flags&2) == 0) break;
                }
                await ToSignal(STGGlobal, STGGlobal.SignalName.end_spell);
                GC.Collect(); // force collect to prevent future lag spikes
            }
            bar_count -= 1;
            STGGlobal.EmitSignal(STGGlobal.SignalName.bar_changed, bar_count);
        }
        STGGlobal.EmitSignal(STGGlobal.SignalName.end_battle);
    }

    // use this function to safely delete the battle controller.
    // do not use QueueFree() directly since that'll free all the bullets.
    public async void kill(){
        ProcessMode = ProcessModeEnum.Disabled;
        STGGlobal.shared_area.Reparent(STGGlobal, false);
        STGGlobal.EmitSignal(STGGlobal.SignalName.stop_all_spawners);
        STGGlobal.clear();
        QueueRedraw();
        await ToSignal(STGGlobal, STGGlobal.SignalName.cleared);
        QueueFree();
    }

    public override void _PhysicsProcess(double delta){
        QueueRedraw();
    }

    public override void _Draw(){
        Parallel.ForEach(STGGlobal.mmpool, mm => {
            mm.multimesh.VisibleInstanceCount = mm.bullets.Count;
            if (mm.multimesh.VisibleInstanceCount == 0) return;
            for (int i = 0; i < mm.bullets.Count; i++){
                STGBulletInstance blt = mm.bullets[i];
                mm.multimesh.SetInstanceTransform2D(i, new Transform2D(blt.direction, blt.position));
                mm.multimesh.SetInstanceCustomData(i, blt.custom_data);
            }
        });
        // canvasitem functions are not thread-safe i think
        foreach (STGMultiMesh mm in STGGlobal.mmpool){
            if (mm.multimesh.VisibleInstanceCount == 0) continue;
            DrawMultimesh(mm.multimesh, mm.texture);
        }
    }

    public void _on_timer_timeout(){

    }

    public void _on_bar_emptied(){
        is_spell_over = true;
        STGGlobal.EmitSignal(STGGlobal.SignalName.end_sequence);
        STGGlobal.EmitSignal(STGGlobal.SignalName.end_spell);
        STGGlobal.EmitSignal(STGGlobal.SignalName.stop_all_spawners);
        STGGlobal.clear();
    }

    public void _on_spell_timed_out(){
        is_spell_over = true;
        STGGlobal.EmitSignal(STGGlobal.SignalName.end_sequence);
        STGGlobal.EmitSignal(STGGlobal.SignalName.end_spell);
        STGGlobal.EmitSignal(STGGlobal.SignalName.stop_all_spawners);
    }

    public void _on_end_sequence(){
        STGGlobal.EmitSignal(STGGlobal.SignalName.stop_all_spawners);
    }

    public void _on_damage_taken(int _life){
        if (_life <= 0){
            STGGlobal.EmitSignal(STGGlobal.SignalName.bar_emptied);
            return;
        }
        if (_life <= hp_threshold){
            STGGlobal.EmitSignal(STGGlobal.SignalName.end_sequence);
            STGGlobal.EmitSignal(STGGlobal.SignalName.stop_all_spawners);
            return;
        }
    }
}
