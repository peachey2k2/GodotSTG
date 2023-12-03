#if TOOLS
using Godot;
using System;
using System.ComponentModel;

[Tool]
public partial class PreviewScene:Control{
    public float hitbox_size;
    public TextureRect Background;
    public TextureRect Bullet;
    public CollisionShape2D Hitbox;
    public TextureRect Outline;

    public override void _Ready(){
        Background = (TextureRect)GetChild(0);
        Bullet = (TextureRect)GetChild(1);
        Outline = (TextureRect)GetChild(2);
        Hitbox = (CollisionShape2D)Outline.GetChild(0);
    }
}
#endif