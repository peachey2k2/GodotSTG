using System.Collections.Generic;
using Godot;

namespace GodotSTG;

public partial class STGMultiMesh:Resource{
    public MultiMesh multimesh = new();
    public Texture2D texture;
    public List<STGBulletInstance> bullets = new();
}