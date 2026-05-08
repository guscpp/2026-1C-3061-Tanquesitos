#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Custom Effects - https://docs.monogame.net/articles/content/custom_effects.html
// High-level shader language (HLSL) - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl
// Programming guide for HLSL - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-pguide
// Reference for HLSL - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reference
// HLSL Semantics - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 DiffuseColor;

float Time = 0;

struct VertexShaderInput
{
	float4 Position : POSITION0; //Posicion original del vertice
	float2 TextureCoordinate : TEXCOORD0; //Coordenada UV que le corresponde
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION; // Posición final calculada en la pantalla.
	float2 TextureCoordinate : TEXCOORD1; // Coordenada de textura pasada para ser interpolada.
};

texture ModelTexture; // El archivo de imagen (textura).

// El Sampler define CÓMO se lee la textura.
sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MagFilter = Linear; // Filtro lineal para que se vea suave al agrandar.
    MinFilter = Linear; // Filtro lineal para que se vea suave al achicar.
    AddressU = Wrap;    // Si la coordenada sale de la imagen, vuelve a empezar (repetición).
    AddressV = Wrap;
};

// Su trabajo principal es transformar coordenadas 3D locales a coordenadas de pantalla.
VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    // 1. Multiplicamos la posición del vértice por la matriz de Mundo (lo movemos al sitio correcto).
    float4 worldPosition = mul(input.Position, World);
    
    // 2. Lo multiplicamos por la matriz de Vista (calculamos dónde está respecto a la cámara).
    float4 viewPosition = mul(worldPosition, View);
    
    // 3. Lo proyectamos (convertimos 3D a la profundidad de la pantalla).
    output.Position = mul(viewPosition, Projection);

    // Pasamos la coordenada de textura directamente al siguiente paso sin cambios.
    output.TextureCoordinate = input.TextureCoordinate;

    return output;
}

// Su trabajo es decidir el color final de cada punto.
float4 MainPS(VertexShaderOutput input) : COLOR
{
    // tex2D toma el Sampler y la coordenada UV para saber qué color de la imagen corresponde a este píxel.
    return tex2D(textureSampler, input.TextureCoordinate);
}

// Define cómo se debe compilar y ejecutar el shader.
technique BasicDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};