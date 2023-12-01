#if TOOLS
using Godot;

[Tool]
public partial class STGBulletPreview : EditorInspectorPlugin{

    TextureRect preview;

    public override bool _CanHandle(GodotObject @object){
        return @object is STGBulletData;
    }

    public override void _ParseBegin(GodotObject @object){
        preview = (TextureRect)((PackedScene)ResourceLoader.Load("res://addons/GodotSTG/preview/PreviewScene.tscn")).Instantiate();
        AddCustomControl(preview);
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide){
        if (name == "texture"){
            preview.Texture = ((STGBulletData)@object).texture;
        }
        return false;
    }
}
#endif