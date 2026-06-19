#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

// Matriz de transformación estándar de la cámara activa (Jugador o Luz)
float4x4 WorldViewProjection;

// Matriz inversa traspuesta del mundo (sirve para rotar las normales sin deformarlas si hay escalado uniformes)
float4x4 InverseTransposeWorld;

// Matriz de la Luz (Combina la vista desde la Luz y su proyección ortográfica/perspectiva)
float4x4 LightViewProjection;
float4x4 DynamicLightViewProjection; // para el mapa dinámico

// Posición en el espacio tridimensional de la fuente de luz
float3 lightPosition;

// Tamaño de la textura del Shadow Map en píxeles (ej: float2(2048.0, 2048.0))
float2 shadowMapSize;

float3 DiffuseColor = float3(1.0, 1.0, 1.0);

float staticDepthRange;
float dynamicDepthRange;

// Constantes matemáticas muy pequeñas para el cálculo del Bias Dinámico.
// Evitan el shadow acne (las lineas raras esas, no el pixelado).
static const float modulatedEpsilon = 0.008;
static const float maxEpsilon = 0.004;

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
	Texture = (ModelTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture shadowMapStatic;
sampler2D shadowMapStaticSampler = sampler_state {
    Texture = (shadowMapStatic);
    MinFilter = Point; // Se usa Point para leer el valor exacto de profundidad sin interpolar incorrectamente los bordes
	MagFilter = Point; 
	MipFilter = Point;
    AddressU = Clamp; 
	AddressV = Clamp;
};

texture shadowMapDynamic;
sampler2D shadowMapDynamicSampler = sampler_state {
    Texture = (shadowMapDynamic);
    MinFilter = Point; 
	MagFilter = Point; 
	MipFilter = Point;
    AddressU = Clamp; 
	AddressV = Clamp;
};

struct DepthPassVertexShaderInput
{
	float4 Position : POSITION0;
};

struct DepthPassVertexShaderOutput
{
	float4 Position : SV_POSITION;       // Posición para la rasterización de la GPU
	float4 ScreenSpacePosition : TEXCOORD1; // Posición duplicada para calcular la profundidad exacta en el píxel
};

struct ShadowedVertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL;
	float2 TextureCoordinates : TEXCOORD0;
};

struct ShadowedVertexShaderOutput
{
	float4 Position : SV_POSITION;          // Posición en la pantalla del jugador
	float2 TextureCoordinates : TEXCOORD0;  // Coordenadas de la textura base
	float4 WorldSpacePosition : TEXCOORD1;   // Posición del píxel en el mundo (para calcular dirección de luz)
	float4 LightSpacePosition : TEXCOORD2;   // Posición del píxel proyectado bajo la perspectiva de la luz
    float4 Normal : TEXCOORD3;              // Normal transformada para los cálculos de iluminación
};

DepthPassVertexShaderOutput DepthVS(in DepthPassVertexShaderInput input)
{
	DepthPassVertexShaderOutput output;
	
	output.Position = mul(input.Position, WorldViewProjection);
	
	output.ScreenSpacePosition = mul(input.Position, WorldViewProjection);
	
	return output;
}

float4 DepthPS(in DepthPassVertexShaderOutput input) : COLOR
{

	float depth = input.ScreenSpacePosition.z / input.ScreenSpacePosition.w;
	
	return float4(depth, depth, depth, 1.0);
}

ShadowedVertexShaderOutput MainVS(in ShadowedVertexShaderInput input)
{
    ShadowedVertexShaderOutput output;
    
    float4 worldPosition = mul(input.Position, World);

    output.WorldSpacePosition = worldPosition;
    
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    
    output.TextureCoordinates = input.TextureCoordinates;
    
    output.LightSpacePosition = mul(output.WorldSpacePosition, LightViewProjection);
    
    output.Normal = mul(float4(input.Normal, 1), InverseTransposeWorld);
    
    return output;
}

float4 ShadowedPCFPS(in ShadowedVertexShaderOutput input) : COLOR
{
    // Coordenadas para el mapa ESTÁTICO
    float4 staticLS = mul(input.WorldSpacePosition, LightViewProjection);
    float3 staticLightSpace = staticLS.xyz / staticLS.w;
    float2 staticCoords = 0.5 * staticLightSpace.xy + float2(0.5, 0.5);
    staticCoords.y = 1.0f - staticCoords.y;

    // Coordenadas para el mapa DINÁMICO
    float4 dynLS = mul(input.WorldSpacePosition, DynamicLightViewProjection);
    float3 dynamicLightSpace = dynLS.xyz / dynLS.w;
    float2 dynamicCoords = 0.5 * dynamicLightSpace.xy + float2(0.5, 0.5);
    dynamicCoords.y = 1.0f - dynamicCoords.y;

    // Bias dinámico
    float3 normal = normalize(input.Normal.rgb);
    float3 lightDirection = normalize(lightPosition - input.WorldSpacePosition.xyz);
	float NdotL = saturate(dot(normal, lightDirection));
    static const float modulatedEpsilon = 0.008;
	static const float minEpsilon = 0.0008;   // antes "maxEpsilon", es el piso
	static const float maxBiasCap = 0.0005;   // techo real para que no se dispare en paredes

	float inclinationBias = clamp(modulatedEpsilon * (1.0 - NdotL), minEpsilon, maxBiasCap);

    float notInStaticShadow = 0.0;
    float notInDynamicShadow = 0.0;
    float2 texelSize = 1.0 / shadowMapSize;

    for (int x = -2; x <= 2; x++)
    {
        for (int y = -2; y <= 2; y++)
        {
            float2 offset = float2(x, y) * texelSize;

            float staticDepth = tex2D(shadowMapStaticSampler, staticCoords + offset).r + inclinationBias;
            notInStaticShadow += step(staticLightSpace.z, staticDepth);

            float dynamicDepth = tex2D(shadowMapDynamicSampler, dynamicCoords + offset).r + inclinationBias;
            notInDynamicShadow += step(dynamicLightSpace.z, dynamicDepth);
        }
    }

    notInStaticShadow /= 25.0;
    notInDynamicShadow /= 25.0;

    float factorSombraFinal = notInStaticShadow * notInDynamicShadow;

    float4 texColor = tex2D(textureSampler, input.TextureCoordinates);
    float4 finalColor = texColor * float4(DiffuseColor, 1.0);

    // Iluminación difusa
    float3 lightDir = normalize(lightPosition - input.WorldSpacePosition.xyz);
    float diffuse = saturate(dot(normal, lightDir));
    float ambientAmount = 0.7;
    float lightAmount = ambientAmount + (1.0 - ambientAmount) * diffuse * factorSombraFinal;
    finalColor.rgb *= lightAmount;

    return finalColor;
}

technique DepthPass
{
	pass Pass0
	{
		VertexShader = compile VS_SHADERMODEL DepthVS();
		PixelShader = compile PS_SHADERMODEL DepthPS();
	}
};


technique DrawShadowedHibrido {
    pass Pass0 {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ShadowedPCFPS();
    }
};