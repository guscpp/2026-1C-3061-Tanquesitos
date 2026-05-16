#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// === MATRICES ===
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 LightViewProjection;

// === ILUMINACIÓN ===
float3 LightDir;
float3 LightColor;
float3 AmbientColor;
float3 EyePos;
float Shininess = 32.0f;

// === TEXTURAS ===
texture ModelTexture;
sampler2D textureSampler = sampler_state {
    Texture = <ModelTexture>;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture ShadowMap;
sampler2D shadowSampler = sampler_state {
    Texture = <ShadowMap>;
    MagFilter = Point;
    MinFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// === ESTRUCTURAS ===
struct VertexShaderInput {
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 UV       : TEXCOORD0;
    float4 Color    : COLOR0;
};

struct VertexShaderOutput {
    float4 Position      : SV_POSITION;
    float3 Normal        : TEXCOORD0;
    float2 UV            : TEXCOORD1;
    float3 WorldPos      : TEXCOORD2;
    float4 LightSpacePos : TEXCOORD3;
    float4 Color         : COLOR0;
};

// === VERTEX SHADER ===
VertexShaderOutput MainVS(VertexShaderInput input) {
    VertexShaderOutput output;
    float4 worldPos = mul(input.Position, World);
    output.WorldPos = worldPos.xyz;
    output.Position = mul(worldPos, mul(View, Projection));
    output.Normal = normalize(mul(input.Normal, (float3x3)World));
    output.UV = input.UV;
    output.LightSpacePos = mul(worldPos, LightViewProjection);
    output.Color = input.Color;
    return output;
}

// === PIXEL SHADER ===
float4 MainPS(VertexShaderOutput input) : COLOR {
    // Vectores normalizados
    float3 N = normalize(input.Normal);
    float3 L = normalize(-LightDir);
    float3 V = normalize(EyePos - input.WorldPos);
    float3 H = normalize(L + V);

    // Componentes de luz Blinn-Phong
    float NdotL = saturate(dot(N, L));
    float NdotH = saturate(dot(N, H));
    float3 diffuse = LightColor * NdotL;
    float3 specular = LightColor * pow(NdotH, Shininess) * step(0.001, NdotL);
    float3 lighting = AmbientColor + diffuse + specular;

    // Cálculo de sombra
    float3 projCoords = input.LightSpacePos.xyz / input.LightSpacePos.w;
    projCoords = projCoords * 0.5 + 0.5;
    
    float currentDepth = projCoords.z;
    float shadowDepth = tex2D(shadowSampler, projCoords.xy).r;
    float shadow = (shadowDepth + 0.002 < currentDepth && 
                    projCoords.x >= 0.0 && projCoords.x <= 1.0 && 
                    projCoords.y >= 0.0 && projCoords.y <= 1.0) ? 1.0 : 0.0;
    float shadowFactor = 1.0 - shadow;

    // Color final
    float3 finalColor = lighting * shadowFactor;
    float4 texColor = tex2D(textureSampler, input.UV);
    return float4(finalColor, 1.0) * texColor * input.Color;
}

// === TÉCNICA PRINCIPAL ===
technique LightingTechnique {
    pass Pass0 {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

// === SHADER DE PROFUNDIDAD (para Shadow Map) ===
struct DepthOutput {
    float4 Position : SV_POSITION;
    float Depth     : TEXCOORD0;
};

DepthOutput DepthVS(float4 position : POSITION0) {
    DepthOutput output;
    output.Position = mul(position, mul(World, LightViewProjection));
    output.Depth = output.Position.z / output.Position.w;
    return output;
}

float4 DepthPS(float depth : TEXCOORD0) : COLOR {
    return float4(depth, depth, depth, 1.0);
}

technique DepthTechnique {
    pass Pass0 {
        VertexShader = compile VS_SHADERMODEL DepthVS();
        PixelShader = compile PS_SHADERMODEL DepthPS();
    }
}