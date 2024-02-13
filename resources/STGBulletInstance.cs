using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

namespace GodotSTG;

[GlobalClass]
public partial class STGBulletInstance:Resource{
    public int bid;
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
        bid = data.bid;
        collision_radius = data.collision_radius;
        lifespan = modifier.lifespan;
        next = modifier.next;
        tweens = modifier.tweens;
        if (data.colorable){
            custom_data = pack_color(modifier.outer_color, modifier.inner_color, modifier.alpha);
        } else {
            custom_data = new Color(1, 1, 1, modifier.alpha + 2);
        }
    }

    private Color pack_color(Color outer, Color inner, float alpha){
        return new(outer * 256 + inner * (float)0.9999, alpha);
    }

    public void set_color(Color outer, Color inner, float alpha){
        custom_data = pack_color(outer, inner, alpha);
    }

    public override string ToString(){
        return $"STGBulletInstance [iid:{GetInstanceId()} bid:{bid} pos:{position}]";
    }
}