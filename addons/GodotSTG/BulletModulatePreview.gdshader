 /*
 / bullet modulator shader written by peachey2k2
 / please use this so i can sue you for money and buy fumos
 / 
 / this does pretty much the same thing as the other shader
 / except it uses shader parameters instead of custom data.
 / you can't have custom data on textures that aren't multimeshes
 / so i have to do this terribleness.
*/ 

// shader type is canvas_item since we're working with 2d here.
shader_type canvas_item;

// uniform variables are exported. we also need to
// clarify the hint so we have a color picker in the editor.
uniform vec3 black : source_color = vec3(0.0);
uniform vec3 white : source_color = vec3(1.0);
uniform float alpha : hint_range(0.0, 1.0) = 1.0;

// vertex() is called for every vertex
void vertex(){
	// modify the alpha channel
	COLOR.a *= alpha;
}

// fragment() is called for every fragment (pixels with some additional info)
void fragment(){
	
	// simple vector math to grayscale values
	float g = (COLOR.r + COLOR.g + COLOR.b) / 3.0;
	
	// use grayscale values to replace blacks and whites
	COLOR.rgb = g * white + (1.0-g) * black;
	
}

