 /*
 / bullet modulator shader written by peachey2k2
 / please use this so i can sue you for money and buy fumos
 / 
 / this shader exists as a way to conveniently change the color of
 / a bullet easily, and only doing it in intended areas. we aren't
 / using CanvasItem.modulate because that'd mess with white parts
 / of the bullets, which we don't want to.
*/ 

// if you're new to shaders and looking here to see an example, you
// might wanna look at BulletModulatePreview.gdshader since it basically
// does the same thing but is more clear, so you can follow it easier.

// shader type is canvas_item since we're working with 2d here.
shader_type canvas_item;

// disable godot's own shadering - this includes things like lighting.
// comment the line below if it causes problems for you.
render_mode unshaded;

// varying means this value is seperate for every vertex.
// it allows us to send data from vertex() to fragment().
varying vec4 custom;
varying vec3 white; 
varying vec3 black;

void vertex(){
	custom = INSTANCE_CUSTOM;
	if (custom.a <= 1.5){
		COLOR.a *= custom.a;
		vec3 temp = floor(custom.rgb);
		black = temp / 256.0;
		white = custom.rgb - temp;
	}
}

void fragment(){
	if (custom.a <= 1.5){
		float g = (COLOR.r + COLOR.g + COLOR.b) / 3.0;
		COLOR.rgb = g * white + (1.0-g) * black;
	} else {
		COLOR.rgba *= custom.rgba;
	}
}