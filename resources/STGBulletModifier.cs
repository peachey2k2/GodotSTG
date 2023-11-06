using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

namespace GodotSTG;

[GlobalClass]
public partial class STGBulletModifier:Resource{

    [Export] public int index {get; set;} = 0;
    [Export] public Color outer_color {get; set;} = Colors.Red;
    [Export] public Color inner_color {get; set;} = Colors.White;
    [Export] public float speed {get; set;}
    [Export] public double lifespan {get; set;} = -1;
    private string _lifecycle; 
    [Export(PropertyHint.MultilineText)] public string lifecycle {
        get{ return _lifecycle; }
        set{
            // I LOVE WRITING PARSERS!!!!! I LOVE WRITING PARSERS!!!!!
            // btw if you aren't using a tool script, godot will run the
            // setter by itself at runtime, when the object is initialized.
            _lifecycle = value;
            if (value == "") return;
            foreach (string cmd in value.Split('\n')){
                if (cmd.StartsWith('-')){
                    // arguments
                    string[] args = cmd.TrimStart('-').Split(' ');
                    switch (args[0]){
                        case "track":
                        case "home":
                        case "sine":
                        default:
                            GD.PrintErr("Lifecycle parsing error");
                            break;
                    }
                } else if (cmd.StartsWith("next")){
                    // destroy instructions
                } else {
                    // tweens
                    string[] args = cmd.TrimEnd('&').Split(' ');
                    if (args.Length < 3){
                        GD.PrintErr("Lifecycle parsing error");
                        return;
                    }
                    BulletTween tween = new();
                    if (cmd.EndsWith('&')){
                        tween.Parallelize = true;
                    }
                    args[0] = args[0].Right(1);
                    switch (cmd.First()){
                        case 's':
                            tween.Property = "magnitude";
                            break;
                        case 'r':
                            tween.Property = "direction";
                            break;
                        case 'p':
                            tween.Property = "position";
                            break;
                        default:
                            GD.PrintErr("Lifecycle parsing error");
                            break;
                    }
                    if (args[0].StartsWith('+')){
                        tween.Additive = true;
                        args[0] = args[0].Right(1);
                    }
                    tween.FinalValue = args[0].ToFloat();
                    tween.Duration = args[1].ToFloat();
                    tween.Transition = (Tween.TransitionType)args[2].ToInt();
                    GD.Print(tween.Easing);
                    if (args.Length > 3) tween.Easing = args[3].ToInt();
                    tweens.Add(tween);
                }
            }
        }
    }
    [Export] public STGBulletModifier next {get; set;}

    // these are automatically set at runtime. dw about them.
    public int id = -1;
    public List<BulletArgument> args = new();
    public List<BulletTween> tweens = new();
}

public class BulletArgument{
    public enum ArgType {}

    public ArgType Argument;
    public int value;
}

public class BulletTween{
    public string Property;
    public bool Additive = false;
    public float FinalValue;
    public float Duration;
    public Tween.TransitionType Transition;
    public int Easing = -1;
    public bool Parallelize = false;
}

public class BulletDestroy{
    // this is gonna be so annoying to implement
    // i dont even wanna work on it ffs
}