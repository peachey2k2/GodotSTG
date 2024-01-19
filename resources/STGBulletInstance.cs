using Godot;
using Godot.Collections;

namespace GodotSTG;

[GlobalClass]
public partial class STGBulletInstance:Resource{
    public int id;
    public float collision_radius;
    public Vector2 position;
    public float direction;
    public float magnitude;
    public float homing = 0;
    public double lifespan;
    public Array<STGTween> tweens;
    public int current = 0;
    public bool grazed = false;
    public Color custom_data;
    public STGShape shape;
    public STGBulletModifier next;

    public STGBulletInstance(STGBulletData data, STGBulletModifier modifier){
        collision_radius = data.collision_radius;
        lifespan = modifier.lifespan;
        next = modifier.next;
        tweens = modifier.tweens;
        custom_data = modifier.outer_color;
        custom_data.A = modifier.inner_color.V;
    }
}