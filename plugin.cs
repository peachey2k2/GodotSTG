#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotSTG;

[Tool]
public partial class plugin:EditorPlugin{

	Dictionary[] settings = {
		new(){
			{"name", "bullet_directory"},
			{"default", "res://addons/GodotSTG/bullets/"},
			{"type", (int)Variant.Type.String}, // int casting cuz appaerently 
			{"hint", (int)PropertyHint.Dir},    // enums aren't real.
			{"hint_string", ""}
		},
		new(){
			{"name", "collision_layer"},
			{"default", 0b10},
			{"type", (int)Variant.Type.Int},
			{"hint", (int)PropertyHint.Layers2DPhysics},
			{"hint_string", ""}
		},
		new(){
			{"name", "removal_margin"},
			{"default", 100},
			{"type", (int)Variant.Type.Int},
			{"hint", (int)PropertyHint.Range},
			{"hint_string", "0,1000,1,or_greater"}
		},
		new(){
			{"name", "pool_size"},
			{"default", 5000},
			{"type", (int)Variant.Type.Int},
			{"hint", (int)PropertyHint.Range},
			{"hint_string", "100,20000,1,or_greater"}
		},
		new(){
			{"name", "graze_radius"},
			{"default", 50},
			{"type", (int)Variant.Type.Float},
			{"hint", (int)PropertyHint.Range},
			{"hint_string", "1,200,0.25,or_greater"}
		},
		new(){
			{"name", "enable_panel_at_start"},
			{"default", false},
			{"type", (int)Variant.Type.Bool},
			{"hint", (int)PropertyHint.None},
			{"hint_string", ""}
		},
		new(){
			{"name", "panel_position"},
			{"default", 0},
			{"type", (int)Variant.Type.Int},
			{"hint", (int)PropertyHint.Enum},
			{"hint_string", "Top Left,Top Right,Bottom Left,Bottom Right"}
		},
		new(){
			{"name", "multimesh_count"},
			{"default", 10},
			{"type", (int)Variant.Type.Int},
			{"hint", (int)PropertyHint.Range},
			{"hint_string", "1,50,1,or_greater"}
		}
	};

	STGBulletPreview preview = new();

	public override void _EnterTree(){
		AddAutoloadSingleton("STGGlobal", "res://addons/GodotSTG/STGGlobal.cs");
		AddInspectorPlugin(preview);
		_setup_settings();
	}

	public override void _ExitTree(){
		RemoveAutoloadSingleton("STGGlobal");
		RemoveInspectorPlugin(preview);
	}

    public override void _DisablePlugin(){
        _clear_settings();
    }

	public void _setup_settings(){
		foreach (Dictionary _setting in settings){
			string _name = _get_setting_path(_setting);
			if (!ProjectSettings.HasSetting(_name)){
				var _default = _setting["default"];
				ProjectSettings.SetSetting(_name, _default);
				ProjectSettings.SetInitialValue(_name, _default);
				ProjectSettings.AddPropertyInfo(new() {
					{"name", _name},
					{"type", _setting["type"]},
					{"hint", _setting["hint"]},
					{"hint_string", _setting["hint_string"]}
				});
			}
		}
		ProjectSettings.Save();
	}

	public string _get_setting_path(Dictionary _setting){
		// return "godotstg/" + (((string)_setting["name"]).StartsWith("sfx_") ? "sfx/" : "general/") + ((string)_setting["name"]).TrimPrefix("sfx_");
		return "godotstg/" + "general/" + (string)_setting["name"];
	}

	public void _clear_settings(){
		foreach (Dictionary _setting in settings){
			ProjectSettings.Clear(_get_setting_path(_setting));
		}
		ProjectSettings.Save();
	}

}
#endif
