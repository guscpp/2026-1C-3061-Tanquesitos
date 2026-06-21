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

float4x4 WorldViewProjection;
float4x4 InverseTransposeWorld;
float4x4 LightViewProjection;

float3 lightPosition;
float2 shadowMapSize;
float3 DiffuseColor = float3(1.0, 1.0, 1.0);

static const float modulatedEpsilon = 0.0008;
static const float maxEpsilon = 0.0003;

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
    MinFilter = Point;
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
	float3 Normal : NORMAL;
};

float normalOffsetScale;

struct DepthPassVertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 ScreenSpacePosition : TEXCOORD1;
};

struct ShadowedVertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL;
	float2 TextureCoordinates : TEXCOORD0;
};

struct ShadowedVertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TextureCoordinates : TEXCOORD0;
	float4 WorldSpacePosition : TEXCOORD1;
	float4 LightSpacePosition : TEXCOORD2;
    float4 Normal : TEXCOORD3;
};

DepthPassVertexShaderOutput DepthVS(in DepthPassVertexShaderInput input)
{
	DepthPassVertexShaderOutput output;

    float4 offsetPosition = input.Position + float4(input.Normal * normalOffsetScale, 0);
    
	output.Position = mul(offsetPosition, WorldViewProjection);
	output.ScreenSpacePosition = mul(offsetPosition, WorldViewProjection);
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

    float4 worldPositionReal = mul(input.Position, World);

    // Posición con offset SOLO para el cálculo de sombra (no afecta el renderizado visual)
    float4 offsetPosition = input.Position + float4(input.Normal * normalOffsetScale, 0);
    float4 worldPositionOffset = mul(offsetPosition, World);

    output.WorldSpacePosition = worldPositionReal;

    float4 viewPosition = mul(worldPositionReal, View);
    output.Position = mul(viewPosition, Projection);

    output.TextureCoordinates = input.TextureCoordinates;

    output.LightSpacePosition = mul(worldPositionOffset, LightViewProjection);

    output.Normal = mul(float4(input.Normal, 1), InverseTransposeWorld);

    return output;
}

float4 ShadowedPCFPS(in ShadowedVertexShaderOutput input) : COLOR
{
	float3 lightSpacePosition = input.LightSpacePosition.xyz / input.LightSpacePosition.w;
	float2 shadowMapTextureCoordinates = 0.5 * lightSpacePosition.xy + float2(0.5, 0.5);
	shadowMapTextureCoordinates.y = 1.0f - shadowMapTextureCoordinates.y;

	float3 normal = normalize(input.Normal.rgb);
	float3 lightDirection = normalize(lightPosition - input.WorldSpacePosition.xyz);
	float inclinationBias = max(modulatedEpsilon * (1.0 - dot(normal, lightDirection)), maxEpsilon);

	float notInStaticShadow = 0.0;
    float notInDynamicShadow = 0.0;
	float2 texelSize = 1.0 / shadowMapSize;

	for (int x = -1; x <= 1; x++)
	{
		for (int y = -1; y <= 1; y++)
		{
			float2 offset = float2(x, y) * texelSize;
            float staticDepth = tex2D(shadowMapStaticSampler, shadowMapTextureCoordinates + offset).r + inclinationBias;
            notInStaticShadow += step(lightSpacePosition.z, staticDepth);
            float dynamicDepth = tex2D(shadowMapDynamicSampler, shadowMapTextureCoordinates + offset).r + inclinationBias;
            notInDynamicShadow += step(lightSpacePosition.z, dynamicDepth);
		}
	}

    notInStaticShadow /= 9.0;
    notInDynamicShadow /= 9.0;
    float factorSombraFinal = notInStaticShadow * notInDynamicShadow;

    float4 texColor = tex2D(textureSampler, input.TextureCoordinates);
    float4 finalColor = texColor * float4(DiffuseColor, 1.0);

    float diffuse = saturate(dot(normal, lightDirection));
    float ambientAmount = 0.6;
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