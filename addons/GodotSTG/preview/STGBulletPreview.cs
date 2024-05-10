#if TOOLS
using System.Threading.Tasks;
using Godot;

namespace GodotSTG;

[Tool]
public partial class STGBulletPreview : EditorInspectorPlugin{

    PreviewScene preview;

    public override bool _CanHandle(GodotObject @object){
        return @object is STGBulletData;
    }

    public override async void _ParseBegin(GodotObject @object){
        // if (@object == null) return; // stupid little bug
        preview = (PreviewScene)((PackedScene)ResourceLoader.Load("res://addons/GodotSTG/preview/PreviewScene.tscn")).Instantiate();
        AddCustomControl(preview);
        (@object as STGBulletData).preview = preview;
        await Task.Delay(1);
        (@object as STGBulletData).UpdateTexture();
        (@object as STGBulletData).UpdateHitbox();
        (@object as STGBulletData).show_hitbox = true;
        (@object as STGBulletData).black = new(0, 0, 0, 1);
        (@object as STGBulletData).white = new(1, 1, 1, 1);
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide){
        if (name == "texture"){
            preview.Bullet.Texture = ((STGBulletData)@object).texture;
        }
        return false;
    }
}
#endif