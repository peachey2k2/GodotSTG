using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using GodotSTG;


public partial class STGGlobal:Node{

    static StringName stg_info = new("stg_info"); // this is here to not create unnecesary strings

    // All the signals are created here. Having them all in one place makes it easier to manage them.
    // Since this script is globally loaded, you can connect to them from anywhere in your game.

    // emitted when the 'start()' method is successfully called.
    [Signal] public delegate void battle_startEventHandler();

    // emitted when a new spell starts.
    [Signal] public delegate void spell_changedEventHandler(STGCustomData data);

    // emitted when switching to the next health bar. returns the new bar count.
    [Signal] public delegate void bar_changedEventHandler(int value);

    // emitted when a sequence is over. used by the plugin itself.
    [Signal] public delegate void end_sequenceEventHandler();

    // emitted when a spell is over. used by the plugin itself.
    [Signal] public delegate void end_spellEventHandler();

    // emitted when the battle is over.
    [Signal] public delegate void end_battleEventHandler();

    // emitted when the screen is cleared of bullets.
    [Signal] public delegate void clearedEventHandler();

    // emitted when a spawner is done spawning bullets. used by the plugin itself.
    // this is probably gonna be useless for you.
    [Signal] public delegate void spawner_doneEventHandler();

    // emitted when a bullet is spawned. returns the spawned bullet.
    [Signal] public delegate void bullet_spawnedEventHandler(STGBulletInstance bullet);

    // emitted when a bullet is grazed. returns the grazed bullet.
    [Signal] public delegate void grazeEventHandler(STGBulletInstance bullet);

    // emitted when the health bar is emptied.
    [Signal] public delegate void bar_emptiedEventHandler();

    // emit this signal to stop all the running spawners. does not stop the battle.
    [Signal] public delegate void stop_all_spawnersEventHandler();

    // emit this signal to send the plugin the health of the enemy.
    // DO NOT USE THIS SIGNAL. use 'update_health()' instead.
    [Signal] public delegate void damage_takenEventHandler(int new_amount);


    private PackedScene area_template;

    public BattleController controller {get; set;}
    public CanvasLayer panel {get; set;}

    // lists for default settings
    private System.Collections.Generic.Dictionary<string, Variant>[] settings = {
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
        },
        new() {
            {"name", "panel_position"},
            {"default", 0},
        },
        new() {
            {"name", "multimesh_count"},
            {"default", 10},
        }
    };

    // settings
    private string BULLET_DIRECTORY;
    private uint COLLISION_LAYER;
    private uint POOL_SIZE;
    private uint REMOVAL_MARGIN;
    private float GRAZE_RADIUS;
    private bool ENABLE_PANEL_AT_START;
    private int PANEL_POSITION;
    private uint MULTIMESH_COUNT;

    // low level tomfuckery
    public List<STGBulletData> bltdata {get; set;} = new();
    public List<STGShape> bpool {get; set;} = new();
    public List<STGBulletInstance> bqueue {get; set;} = new();
    public List<STGMultiMesh> mmpool {get; set;} = new();
    private Area2D _shared_area;
    public Area2D shared_area {
        get{ return _shared_area; }
        private set{
            area_rid = value.GetRid();
            _shared_area = value;
            shared_area.CollisionLayer = COLLISION_LAYER;
        }
    }
    public Rid area_rid {get; private set;}
    private Rect2 _arena_rect {get; set;}
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
    private const float TIMER_START = 10000000;
    private float clock;
    private float clock_real;
    private SceneTreeTimer clock_timer;
    private SceneTreeTimer clock_real_timer;
    public ulong fps {private set; get;}
    private ulong _fps = 1;
    public ulong start = Time.GetTicksUsec();
    public ulong end;

    private bool exiting = false;
    public static STGGlobal Instance { get; private set; }

    public STGGlobal(){
        // pull the settings from the project settings for ease of access
        foreach (System.Collections.Generic.Dictionary<string, Variant> _setting in settings){
            Set(((string)_setting["name"]).ToUpper(), ProjectSettings.GetSetting("godotstg/general/" + _setting["name"], _setting["default"]));
        }
    }

    public override async void _Ready(){
        // epic self-reference to access the singleton from everywhere
        Instance = this;

        // there is no @onready in c# :sadge: 
        area_template = (PackedScene)ResourceLoader.Load("res://addons/GodotSTG/resources/shared_area.tscn");

        // loading and preparing all the bullets
        foreach (string file in DirAccess.GetFilesAt(BULLET_DIRECTORY)){
            // builds use .remap extension so that is trimmed here
            // you can look at this issue for more info: https://github.com/godotengine/godot/issues/66014
            // also this will probably change in a later release for the engine
            bltdata.Add((STGBulletData)ResourceLoader.Load((BULLET_DIRECTORY + "/" + file).TrimSuffix(".remap")));
            bltdata.Last().bid = bltdata.Count - 1;
            mmpool.Add(new(){
                texture = bltdata.Last().texture,
                multimesh = new(){
                    UseCustomData = true,
                    VisibleInstanceCount = -1,
                    Mesh = new QuadMesh(){
                        Size = bltdata.Last().texture.GetSize()
                    },
                    InstanceCount = (int)POOL_SIZE,
                }
            });
            // if (!bltdata.Last().colorable){
            //     for (int i = 0; i < POOL_SIZE; i++){
            //         mmpool.Last().multimesh.SetInstanceCustomData(i, new Color(-1, -1, -1, -1));
            //     }
            // }
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

        // panel
        panel = (CanvasLayer)((PackedScene)ResourceLoader.Load("res://addons/GodotSTG/panel.tscn")).Instantiate();
        Panel panel_panel = (Panel)panel.GetNode("Panel"); // panel panel panel panel
        switch (PANEL_POSITION){
            case 0:
                panel_panel.Position = new Vector2(0, 0);
                panel_panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                break;
            case 1:
                panel_panel.Position = new Vector2(-panel_panel.Size.X, 0);
                panel_panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
                break;
            case 2:
                panel_panel.Position = new Vector2(0,-panel_panel.Size.Y);
                panel_panel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
                break;
            case 3:
                panel_panel.Position = new Vector2(-panel_panel.Size.X, -panel_panel.Size.Y);
                panel_panel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
                break;
        }
        if (ENABLE_PANEL_AT_START) panel.Show();
        AddChild(panel);
        Label PoolSize = (Label)panel.GetNode("Panel/VBoxContainer/PoolSize/count");
        Label Pooled   = (Label)panel.GetNode("Panel/VBoxContainer/Pooled/count");
        Label Active   = (Label)panel.GetNode("Panel/VBoxContainer/Active/count");
        Label Bullets  = (Label)panel.GetNode("Panel/VBoxContainer/Bullets/count");
        Label FPS      = (Label)panel.GetNode("Panel/VBoxContainer/FPS/count");

        PoolSize.Text = POOL_SIZE.ToString();
        while (true){
            await Task.Delay(250);
            Pooled  .Text = bpool.Count.ToString();
            Active  .Text = bullet_count.ToString();
            Bullets .Text = bltdata.Count.ToString();
            FPS     .Text = fps.ToString();
        }
    }

    // messy fps calculation
    public override void _Process(double delta){
        end = Time.GetTicksUsec();
        if (end - start < 1000000){
            _fps += 1;
        } else {
            start = end;
            fps = _fps;
            _fps = 1;
        }
    }

    // i got the idea on how to optimize this from this nice devlog. it's pretty clean and detailed.
    // also their game looks pretty cool too, so check it out if you have the time.
    // https://worldeater-dev.itch.io/bittersweet-birthday/devlog/210789/howto-drawing-a-metric-ton-of-bullets-in-godot
    public void create_bullet(STGBulletInstance data){
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
        mmpool[data.bid].bullets.Add(data);
        EmitSignal(SignalName.bullet_spawned, data);
    }

    public STGBulletInstance configure_bullet(STGBulletInstance data){
        STGBulletModifier mod = data.next;
        data.lifespan = mod.lifespan > 0 ? mod.lifespan : 999999;
        mmpool[data.bid].bullets.Remove(data);
        mmpool[mod.id].bullets.Add(data);
        data.next = mod.next;
        return data;
    }

    public override void _UnhandledInput(InputEvent @event){
        // programmer challenge: if you can't make out what this line does, quit programming lmao (jk (unless...))
        if (InputMap.HasAction(stg_info) && Input.IsActionJustPressed(stg_info)) panel.Visible = !panel.Visible;
    }

    // processing the bullets here.
    public override void _PhysicsProcess(double delta){
        if (controller == null) return;
        Vector2 player_pos = controller.player.Position;
        bqueue.Clear();
        Parallel.ForEach(mmpool, mm => {
            Parallel.ForEach(mm.bullets, blt => {
                if (blt.lifespan >= 0) blt.lifespan -= delta;
                else bqueue.Add(blt);
                foreach (STGTween tw in blt.tweens){
                    if (blt.current < tw.list.Count){
                        blt.Set(tw.property_str, tw.list[blt.current] + (float)(tw.mode == STGTween.TweenMode.Add ? blt.Get(tw.property_str) : 0));
                        blt.current++;
                    }
                }
                // home the bullet if it's homing
                blt.direction = Clamp(blt.position.AngleToPoint(player_pos), blt.direction - blt.homing, blt.direction + blt.homing);
                // move the bullet
                blt.position += Vector2.Right.Rotated(blt.direction) * blt.magnitude * (float)delta;
                Transform2D t = new(0, blt.position);
                // remove if out of bounds
                if (!arena_rect_margined.HasPoint(blt.position)){
                    bqueue.Add(blt);
                    blt.next = null;
                }
                // check for grazes
                if (controller.player.ProcessMode != ProcessModeEnum.Disabled && !blt.grazed && blt.position.DistanceTo(player_pos) - blt.collision_radius < GRAZE_RADIUS){
                    blt.grazed = true;
                    CallDeferred(MethodName.EmitSignal, SignalName.graze, blt);
                }
                PhysicsServer2D.AreaSetShapeTransform(area_rid, blt.shape.idx, t);
            });
        });
        for (int i = 0; i < bqueue.Count; i++){
            STGBulletInstance blt = bqueue[i];
            if (blt.next == null){
                PhysicsServer2D.AreaSetShapeDisabled(area_rid, blt.shape.idx, true);
                mmpool[blt.bid].bullets.Remove(blt);
                bpool.Add(blt.shape);
            } else {
                blt = configure_bullet(blt);
            }
        }
        bqueue.Clear();
        bullet_count = (int)POOL_SIZE - bpool.Count;
    }

    private static float Clamp(float value, float min, float max){
        return value >= max ? max : (value <= min ? min : value);
    }

    public void clear(){
        Parallel.ForEach(mmpool, mm => {
            if (mm.bullets.Count == 0) return;
            foreach (STGBulletInstance blt in mm.bullets){
                PhysicsServer2D.AreaSetShapeDisabled(area_rid, blt.shape.idx, true);
                bpool.Add(blt.shape);
            }
            mm.bullets.Clear();
        });
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
        GetTree().Paused = true;
        //this somehow leaks at exit. memory management is hard. sorry. #todo: fix
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

    public void update_health(int new_amount){
        // okay this is where all logic and reason dies in agony
        // idk why, but calling the signal directly from gdscript can and will crash your game.
        // so you have to call it from c# instead.
        // also it has to be called deferred due to another issue
        CallDeferred(MethodName.EmitSignal, SignalName.damage_taken, new_amount);
    }
}