#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 LightDirection;
float3 LightColor;
float3 AmbientColor;
float3 EyePosition;
float3 DiffuseColor = float3(1.0, 1.0, 1.0); // Default blanco para no oscurecer la textura
float Shininess = 32.0;

// --- SOPORTE DE TEXTURA ---
texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = <ModelTexture>;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0; // Requerido para mapear la textura
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : TEXCOORD0;
    float3 WorldPosition : TEXCOORD1;
    float2 TextureCoordinate : TEXCOORD2; // Se pasa al Pixel Shader
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    float4 worldPosition = mul(input.Position, World);
    output.WorldPosition = worldPosition.xyz;
    output.Position = mul(worldPosition, mul(View, Projection));
    
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)World));
    output.TextureCoordinate = input.TextureCoordinate;
    
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float3 N = normalize(input.WorldNormal);
    float3 L = normalize(-LightDirection);
    float3 V = normalize(EyePosition - input.WorldPosition);
    float3 H = normalize(L + V);
    
    float NdotL = saturate(dot(N, L));
    float NdotH = saturate(dot(N, H));
    
    float3 ambient = AmbientColor;
    float3 diffuse = LightColor * NdotL;
    float3 specular = LightColor * pow(NdotH, Shininess) * step(0.001, NdotL);
    
    float3 lighting = ambient + diffuse + specular;
    
    // Muestreamos el color de la textura en este píxel
    float4 texColor = tex2D(textureSampler, input.TextureCoordinate);
    
    // Combinamos la iluminación con la textura y el color difuso base
    float3 finalColor = lighting * texColor.rgb * DiffuseColor;
    
    return float4(finalColor, texColor.a);
}

technique BlinnPhongTechnique
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}