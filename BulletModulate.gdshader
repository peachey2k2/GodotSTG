 /*
 / bullet modulator shader written by peachey2k2
 / please use this so i can sue you for money and buy fumos
 / 
 / this shader exists as a way to conveniently change the color of
 / a bullet easily, and only doing it in intended areas. we aren't
 / using CanvasItem.modulate because that'd mess with white parts
 / of the bullets, which we don't want to.
*/ 

// shader type is canvas_item since we're working with 2d here.
shader_type canvas_item;

// disable godot's own shadering - this includes things like lighting.
// comment the line below if it causes problems for you.
render_mode unshaded;

// varying means this value is seperate for every vertex.
// it allows us to send data from vertex() to fragment().
varying vec4 custom;

// you probably know what a constant is.
const vec3 WHITE = vec3(1, 1, 1);

// vertex() is called for every vertex (triangles that make up the mesh)
void vertex(){
	custom = INSTANCE_CUSTOM;
}

// fragment() is called for every fragment (pixels with some additional info)
void fragment(){
	// color.a should be set to -1 if colorable is set to false.
	// so in that case, custom data is just used for modulation.
	if (custom.a >= 0.0){
		float g = (COLOR.r + COLOR.g + COLOR.b) / 3.0;
		COLOR.rgb = g * WHITE * custom.a + (1.0-g) * custom.rgb;
	} else {
		COLOR.rgb *= custom.rgb;
	}
}