using System.Data.Common;
using System.Transactions;
using System.Windows.Markup;
using Godot;
using Godot.Collections;

namespace GodotSTG;

[GlobalClass, Icon("res://addons/GodotSTG/assets/bulletdata.png"), Tool]
public partial class STGBulletData:Resource{

    [ExportCategory("STGBulletData")]
    private Texture2D _texture;
    [Export] public Texture2D texture{
        set{
            _texture = value;
        #if TOOLS
            if (IsInstanceValid(preview)){
                UpdateTexture();
                UpdateHitbox();
            }
        #endif
        }
        get{ return _texture; }
    }
    private float _collision_radius;
    [Export] public float collision_radius{
        set{
            _collision_radius = value;
        #if TOOLS
            if (IsInstanceValid(preview)){
                UpdateHitbox();
            }
        #endif
        }
        get{ return _collision_radius; }
    }
    [Export] public bool colorable {get; set;} = true;

#if TOOLS
    public PreviewScene preview;

    [ExportCategory("Preview")]
    private Color _black = new(0, 0, 0, 1);
    [Export(PropertyHint.ColorNoAlpha)] public Color black{
        set{
            _black = value;
            if (IsInstanceValid(preview)){
                UpdateColor();
            }
        }
        get{ return _black; }
    }
    private Color _white = new(1, 1, 1, 1);
    [Export(PropertyHint.ColorNoAlpha)] public Color white{
        set{
            _white = value;
            if (IsInstanceValid(preview)){
                UpdateColor();
            }
        }
        get{ return _white; }
    }
    private float _alpha = 1;
    [Export(PropertyHint.Range, "0,1")] public float alpha{
        set{
            _alpha = value;
            if (IsInstanceValid(preview)){
                UpdateColor();
            }
        }
        get{ return _alpha; }
    }
    private bool _show_hitbox = true;
    [Export] public bool show_hitbox{
        set{
            _show_hitbox = value;
            if (IsInstanceValid(preview)){
                preview.Hitbox.Visible = value;
            }
        }
        get{ return _show_hitbox; }
    }

    public void UpdateTexture(){
        preview.Bullet.Texture = texture;
    }
    public void UpdateHitbox(){
        float radius = collision_radius * (160 / preview.Bullet.Texture.GetSize().X);
        ((CircleShape2D)preview.Hitbox.Shape).Radius = radius;
    }

    public void UpdateColor(){
        ((ShaderMaterial)preview.Bullet.Material).SetShaderParameter("black", black);
        ((ShaderMaterial)preview.Bullet.Material).SetShaderParameter("white", white);
        ((ShaderMaterial)preview.Bullet.Material).SetShaderParameter("alpha", alpha);
    }
#endif

    public int bid;
}