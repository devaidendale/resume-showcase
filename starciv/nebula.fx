//------- Constants --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;

//------- Texture Samplers --------

Texture xTexture1;
sampler TextureSampler1 = sampler_state { texture = <xTexture1>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = wrap; AddressV = wrap;};

//------- Technique: Textured --------
struct TexVertexToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
    //float LightingFactor: TEXCOORD0;
    float2 TextureCoords: TEXCOORD1;
};

struct TexPixelToFrame
{
    float4 Color : COLOR0;
};

TexVertexToPixel TexturedVS( float4 inPos : POSITION, float3 inNormal: NORMAL0, float2 inTexCoords: TEXCOORD0)
{	
	TexVertexToPixel Output = (TexVertexToPixel)0;
	float4x4 preViewProjection = mul (xView, xProjection);
	float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    
	Output.Position = mul(inPos, preWorldViewProjection);	
	Output.TextureCoords = inTexCoords;
	
	
	Output.Color = float4(1.0f, 0.0f, 0.0f, 1);
    
	return Output;     
}

TexPixelToFrame TexturedPS(TexVertexToPixel PSIn) 
{
	TexPixelToFrame Output = (TexPixelToFrame)0;
	float4 returnColor = 0;
	
	float4 NoiseColor = tex2D(TextureSampler1, PSIn.TextureCoords);
	
	Output.Color.rgba = NoiseColor;
	Output.Color.a = 0.515f;
	//Output.Color.rgb *= saturate(PSIn.LightingFactor + xAmbient);

	return Output;
}

technique Textured
{
	pass Pass0
    {   
    	VertexShader = compile vs_3_0 TexturedVS();
        PixelShader  = compile ps_3_0 TexturedPS();
    }
}
