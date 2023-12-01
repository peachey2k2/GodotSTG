#if TOOLS
using Godot;
using Godot.Collections;

[Tool]
public partial class BulletPreview:EditorResourcePreviewGenerator{

    public override bool _CanGenerateSmallPreview(){
        return false;
    }

    public override Texture2D _Generate(Resource resource, Vector2I size, Dictionary metadata){
        return ((STGBulletData)resource).texture;
    }
}
#endif