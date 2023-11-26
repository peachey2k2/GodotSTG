using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using GodotSTG;

public partial class STGGlobal:Node{

    static StringName stg_info = new("stg_info");

    [Signal] public delegate void battle_startEventHandler();
    [Signal] public delegate void shield_changedEventHandler(int value);
    [Signal] public delegate void spell_name_changedEventHandler(string value);
    [Signal] public delegate void bar_changedEventHandler(int value);
    [Signal] public delegate void life_changedEventHandler(Array<int> values, Array<Color> colors);
    [Signal] public delegate void end_sequenceEventHandler();
    [Signal] public delegate void end_spellEventHandler();
    [Signal] public delegate void end_battleEventHandler();
    [Signal] public delegate void stop_spawnerEventHandler();
    [Signal] public delegate void clearedEventHandler();
    [Signal] public delegate void spawner_doneEventHandler();

    [Signal] public delegate void bar_emptiedEventHandler();
    [Signal] public delegate void damage_takenEventHandler(int new_amount);

    private PackedScene area_template;
    private Texture2D remove_template;

    public BattleController controller {get; set;}
    public CanvasLayer panel;

    // lists for default settings
    System.Collections.Generic.Dictionary<string, Variant>[] settings = {
        new() {
            {"name", "bullet_directory"},
            {"default", "res://addons/GodotSTG/bullets/"},
        },
        new() {
            {"name", "collision_layer"},
            {"default", 0b10},
        },
        new() {
            {"name", "pool_size"},
            {"default", 5000},
        },
        new() {
            {"name", "removal_margin"},
            {"default", 100},
        },
        new() {
            {"name", "graze_radius"},
            {"default", 50},
        },
        new() {
            {"name", "enable_panel_at_start"},
            {"default", false},
        }
    };
    System.Collections.Generic.Dictionary<string, Variant>[] sounds = {
        // new() {
        //     {"name", "spawn"},
        //     {"default", "res://addons/GodotSTG/assets/spawn.ogg"},
        // },
        new() {
            {"name", "graze"},
            {"default", "res://addons/GodotSTG/assets/graze.ogg"},
        }
    };

    // settings
    private string BULLET_DIRECTORY;
    private uint COLLISION_LAYER;
    private uint POOL_SIZE;
    private uint REMOVAL_MARGIN;
    private float GRAZE_RADIUS;
    private bool ENABLE_PANEL_AT_START;
    // private AudioStream SFX_SPAWN;
    private AudioStream SFX_GRAZE;

    // low level tomfuckery
    public List<STGBulletData> blts = new();
    public List<STGShape> bpool = new();
    public List<STGBulletData> bqueue = new();
    public List<STGBulletData> bltdata = new();
    public List<Texture2D> textures = new();
    public List<STGBulletData> brem = new();

    private Area2D _shared_area;
    public Area2D shared_area {
        get{ return _shared_area; }
        set{
            area_rid = value.GetRid();
            _shared_area = value;
            shared_area.CollisionLayer = COLLISION_LAYER;
        }
    }
    public Rid area_rid {private set; get;}
    private Rect2 _arena_rect;
    public Rect2 arena_rect {
        get{ return _arena_rect; }
        set{
            _arena_rect = value;
            arena_rect_margined = value.Grow(REMOVAL_MARGIN);
        }
    }
    public Rect2 arena_rect_margined {get; set;}
    public int bullet_count {get; set;} = 0;

    // clocks
    const float TIMER_START = 10000000;
    public float clock;
    public float clock_real;
    public SceneTreeTimer clock_timer;
    public SceneTreeTimer clock_real_timer;
    public ulong fps {private set; get;}
    private ulong _fps = 1;
    public ulong start = Time.GetTicksUsec();
    public ulong end;
    public int graze_counter {private set; get;}

    private static float fdelta = 0.016667F;
    private bool exiting = false;
    public static STGGlobal Instance { get; private set; }

    public STGGlobal(){
        foreach (System.Collections.Generic.Dictionary<String, Variant> _setting in settings){
            Set(((string)_setting["name"]).ToUpper(), ProjectSettings.GetSetting("godotstg/general/" + _setting["name"], _setting["default"]));
        }
        foreach (System.Collections.Generic.Dictionary<String, Variant> _setting in sounds){
            Set(("SFX_" + (string)_setting["name"]).ToUpper(), ResourceLoader.Load((string)ProjectSettings.GetSetting("godotstg/sfx/" + _setting["name"], _setting["default"])));
        }
    }

    // AudioStreamPlayer spawn_audio;
    AudioStreamPlayer graze_audio;

    public override async void _Ready(){
        Instance = this; // epic self-reference to access the singleton from everywhere
        battle_start += _on_battle_start;

        // there is no @onready in c# :sadge: 
        area_template = (PackedScene)ResourceLoader.Load("res://addons/GodotSTG/resources/shared_area.tscn");
        remove_template = (Texture2D)ResourceLoader.Load("res://addons/GodotSTG/assets/remove.png");

        foreach (string file in DirAccess.GetFilesAt(BULLET_DIRECTORY)){
            bltdata.Add((STGBulletData)ResourceLoader.Load((BULLET_DIRECTORY + "/" + file).TrimSuffix(".remap"))); // builds use .remap extension so that is trimmed here
            // you can look at this issue for more info: https://github.com/godotengine/godot/issues/66014
            // also this will probably change in a later release for the engine
        }

        // pooling lol
        shared_area = (Area2D)area_template.Instantiate(); // THIS MOTHERFUCKER...
        AddChild(shared_area);
        for (int i = 0; i < POOL_SIZE; i++){
            Rid shape_rid = PhysicsServer2D.CircleShapeCreate();
            PhysicsServer2D.AreaAddShape(area_rid, shape_rid);
	    	PhysicsServer2D.AreaSetShapeDisabled(area_rid, i, true);
            bpool.Add(new STGShape(shape_rid, i));
        }

        // global clocks cuz yeah
        clock_timer      = GetTree().CreateTimer(TIMER_START, false);
        clock_real_timer = GetTree().CreateTimer(TIMER_START, true);

        // audio setup
        // spawn_audio = new(){
        //     MaxPolyphony = 100,
        //     Stream = SFX_SPAWN,
        //     PitchScale = 1.0F,
        //     VolumeDb = -5 // ah yes, negative sound
        // };
        graze_audio = new(){
            MaxPolyphony = 50,
            Stream = SFX_GRAZE,
            PitchScale = 1.0F,
            VolumeDb = -20 // ah yes, negative sound
        };
        // AddChild(spawn_audio);
        AddChild(graze_audio);

        // panel
        panel = (CanvasLayer)((PackedScene)ResourceLoader.Load("res://addons/GodotSTG/panel.tscn")).Instantiate();
        if (ENABLE_PANEL_AT_START) panel.Show();
        AddChild(panel);
        Label PoolSize = (Label)panel.GetNode("Panel/VBoxContainer/PoolSize/count");
        Label Pooled   = (Label)panel.GetNode("Panel/VBoxContainer/Pooled/count");
        Label Active   = (Label)panel.GetNode("Panel/VBoxContainer/Active/count");
        Label Removing = (Label)panel.GetNode("Panel/VBoxContainer/Removing/count");
        Label Textures = (Label)panel.GetNode("Panel/VBoxContainer/Textures/count");
        Label FPS      = (Label)panel.GetNode("Panel/VBoxContainer/FPS/count");

        PoolSize.Text = POOL_SIZE.ToString();
        while (true){
            await Task.Delay(250);
            Pooled  .Text = bpool   .Count.ToString();
            Active  .Text = blts    .Count.ToString();
            Removing.Text = brem    .Count.ToString();
            Textures.Text = textures.Count.ToString();
            FPS     .Text = fps           .ToString();
        }
    }

    void _on_battle_start(){
        graze_counter = 0;
    }


    public void _property_changed(){

    }

    // messy fps calculation
    public override void _Process(double delta){
        end = Time.GetTicksUsec();
        if (end - start < 500000){
            _fps += 1;
        } else {
            start = end;
            fps = _fps * 2;
            _fps = 1;
        }
    }

    // i got the idea on how to optimize this from this nice devlog. it's pretty clean and detailed.
    // also their game looks pretty cool too, so check it out if you have the time.
    // https://worldeater-dev.itch.io/bittersweet-birthday/devlog/210789/howto-drawing-a-metric-ton-of-bullets-in-godot
    public void create_bullet(STGBulletData data){
        GodotSTG.Debug.Assert(bpool.Count > 0, "Pool is out of bullets.");
        STGShape shape = bpool.Last();
        GodotSTG.Debug.Assert(shape.rid.IsValid, "Shape RID is invalid.");
        bpool.RemoveAt(bpool.Count - 1);
        Transform2D t = new(0, data.position);
        data.shape = shape;
        PhysicsServer2D.AreaGetShape(area_rid, shape.idx);
        PhysicsServer2D.ShapeSetData(shape.rid, data.collision_radius);
        PhysicsServer2D.AreaSetShapeTransform(area_rid, shape.idx, t);
        PhysicsServer2D.AreaSetShapeDisabled(area_rid, shape.idx, false);
        if (data.lifespan <= 0) data.lifespan = 9999999;
        blts.Add(data);
        // spawn_audio.Play();
    }

    public STGBulletData configure_bullet(STGBulletData data){
        STGBulletModifier mod = data.next;
        data.lifespan = mod.lifespan > 0 ? mod.lifespan : 999999;
        data.texture = textures[mod.id];
        data.next = mod.next;
        return data;
    }

    public override void _UnhandledInput(InputEvent @event){
        if (InputMap.HasAction(stg_info) && Input.IsActionJustPressed(stg_info)) panel.Visible = !panel.Visible;
    }

    // processing the bullets here.
    public override void _PhysicsProcess(double delta){
        if (controller == null) return;
        Vector2 player_pos = controller.player.Position;
        bqueue.Clear();
        Parallel.ForEach(blts, blt => {
            if (blt.lifespan >= 0) blt.lifespan -= fdelta;
            else bqueue.Add(blt);
            foreach (STGTween tw in blt.tweens){
                if (blt.current < tw.list.Count){
                    blt.Set(tw.property_str, tw.list[blt.current] + (float)(tw.mode == STGTween.TweenMode.Add ? blt.Get(tw.property_str) : 0));
                    blt.current++;
                }
            }
            blt.direction = Clamp(blt.position.AngleToPoint(player_pos), blt.direction - blt.homing, blt.direction + blt.homing);
            blt.position += Vector2.Right.Rotated(blt.direction) * blt.magnitude * fdelta;
            Transform2D t = new(0, blt.position);
            if (!arena_rect_margined.HasPoint(blt.position)){
                bqueue.Add(blt);
                blt.next = null;
            }
            if (!blt.grazed && blt.position.DistanceTo(player_pos) - blt.collision_radius < GRAZE_RADIUS){
                blt.grazed = true;
                graze_counter++;
                graze_audio.CallDeferred(AudioStreamPlayer.MethodName.Play);
            }
            PhysicsServer2D.AreaSetShapeTransform(area_rid, blt.shape.idx, t);
        });
        foreach (STGBulletData blt in bqueue){
            if (blt.next == null){
                PhysicsServer2D.AreaSetShapeDisabled(area_rid, blt.shape.idx, true);
                blt.texture = remove_template;
                blt.lifespan = 0.5;
                blts.Remove(blt);
                brem.Add(blt);
            } else {
               blts[blts.IndexOf(blt)] = configure_bullet(blt);
            }
        }
        bqueue.Clear();
        Parallel.ForEach(brem, blt => {
            if (blt.lifespan >= 0) blt.lifespan -= delta;
            else {
                bqueue.Add(blt);
            }
        });
        foreach (STGBulletData blt in bqueue){
            brem.Remove(blt);
            bpool.Add(blt.shape);
        }
        bullet_count = blts.Count;
    }

    private static float Clamp(float value, float min, float max){
        if (value >= max) return max;
        if (value <= min) return min;
        return value;
    }

    public void create_texture(STGBulletModifier mod){
        if (mod.id != -1) return; // #todo: also check whether this exact texture is already saved (same index and colors)
        Texture2D tex = (Texture2D)bltdata[mod.index].texture.Duplicate(); // lol
        if (tex is GradientTexture2D){
            GradientTexture2D gradientTex = tex as GradientTexture2D;
            gradientTex.Gradient = gradientTex.Gradient.Duplicate() as Gradient;
            gradientTex.Gradient.Colors = new[] {mod.inner_color, mod.inner_color, mod.outer_color, mod.outer_color, Colors.Transparent};
        }
        mod.id = textures.Count;
        textures.Add(tex);
    }

    public void clear(){
        EmitSignal(SignalName.stop_spawner);
        foreach (STGBulletData blt in blts){
            PhysicsServer2D.AreaSetShapeDisabled(area_rid, blt.shape.idx, true);
            bpool.Add(blt.shape);
        }
        blts.Clear();
        EmitSignal(SignalName.cleared);
    }

    public Vector2 lerp4arena(Vector2 weight){
        return new Vector2(
            Mathf.Lerp(arena_rect.Position.X, arena_rect.End.X, weight.X),
            Mathf.Lerp(arena_rect.Position.Y, arena_rect.End.Y, weight.Y)
        );
    }

    public override void _Notification(int what){
        if (what == NotificationWMCloseRequest && !exiting) exit();
    }

    public void exit(){
        exiting = true;
        //this leaks at exit. memory management is hard. sorry. #todo: fix
        GetTree().Paused = true;
        for (int i = PhysicsServer2D.AreaGetShapeCount(area_rid); i > -1; i--){
            PhysicsServer2D.FreeRid(PhysicsServer2D.AreaGetShape(area_rid, i));
        }
        GD.Print("Exited.");
        GetTree().Quit();
    }

    // this will always return how much time has passed since the game started.
    public float time(bool count_pauses = true){
        if (count_pauses) {return (float)(TIMER_START - clock_real_timer.TimeLeft);}
        else              {return (float)(TIMER_START - clock_timer.TimeLeft);}
    }

}