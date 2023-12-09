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

// fuck lighting tbh
render_mode unshaded;

// uniform variables are exported. we also need to
// clarify the hint so we have a color picker in the editor.
// both colors and coords use vectors. keep that in mind.
uniform vec3 black : source_color = vec3(0.0);
uniform vec3 white : source_color = vec3(1.0);

// fragment shaders run for every pixel.
void fragment(){
	
	// simple vector math to grayscale values
	float g = (COLOR.r + COLOR.g + COLOR.b) / 3.0;
	
	// use grayscale values to replace blacks and whites
	COLOR.rgb = g * white + (1.0-g) * black;
}