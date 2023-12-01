#if TOOLS
using Godot;
using System;

[Tool]
public partial class PreviewScene:TextureRect{
    public PreviewScene(){
        CustomMinimumSize = new(64, 64);
    }
}
#endif