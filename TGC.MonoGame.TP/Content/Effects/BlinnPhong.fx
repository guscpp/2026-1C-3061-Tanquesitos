// ==========================================
// PARAMETROS GLOBALES
// ==========================================
float4x4 World;
float4x4 View;
float4x4 Projection;

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = <ModelTexture>;
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float3 DiffuseColor;
float3 LightDirection;
float3 LightColor;
float3 AmbientColor;
float3 EyePosition;
float Shininess;

// ==========================================
// PARAMETROS DE DEFORMACION
// ==========================================
int IsDeformable;
float ImpactRadius;

// Array de hasta 6 impactos. 
// XYZ = Posicion en el mundo, W = Profundidad de la deformacion
float4 Impacts[6];

// Variables dummy para mantener compatibilidad con el menu (GameStateManager)
float HasImpact;
float3 ImpactPointWorld;

// ==========================================
// VERTEX SHADER
// ==========================================
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 Normal : TEXCOORD1;
    float2 TextureCoordinate : TEXCOORD0;
    float3 WorldPosition : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Posicion y Normal en espacio de mundo
    float4 worldPosition = mul(input.Position, World);
    float3 worldNormal = normalize(mul(input.Normal, (float3x3) World));

    // --- LOGICA DE DEFORMACION MULTIPLE ---
    if (IsDeformable > 0)
    {
        float totalDeformation = 0;
        
        // Iteramos sobre los 6 posibles impactos
        for (int i = 0; i < 6; i++)
        {
            // Si la profundidad (W) es mayor a 0, el impacto esta activo
            if (Impacts[i].w > 0)
            {
                float dist = distance(worldPosition.xyz, Impacts[i].xyz);
                
                // Si el vertice esta dentro del radio de impacto
                if (dist < ImpactRadius)
                {
                    // Calculamos un factor de suavizado (1 en el centro, 0 en el borde)
                    float factor = 1.0 - (dist / ImpactRadius);
                    
                    // Acumulamos la deformacion total
                    totalDeformation += factor * Impacts[i].w;
                }
            }
        }
        
        // Desplazamos el vertice hacia adentro a lo largo de su normal
        worldPosition.xyz -= worldNormal * totalDeformation;
    }

    // Posicion final en pantalla
    output.WorldPosition = worldPosition.xyz;
    output.Position = mul(worldPosition, View);
    output.Position = mul(output.Position, Projection);
    
    output.Normal = worldNormal;
    output.TextureCoordinate = input.TextureCoordinate;

    return output;
}

// ==========================================
// PIXEL SHADER (Blinn-Phong)
// ==========================================
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 normal = normalize(input.Normal);
    float3 lightDir = normalize(LightDirection);
    float3 viewDir = normalize(EyePosition - input.WorldPosition);
    float3 halfVec = normalize(lightDir + viewDir);

    float4 texColor = tex2D(textureSampler, input.TextureCoordinate);
    float3 surfaceColor = texColor.rgb * DiffuseColor;

    // 1. Ambient
    float3 ambient = AmbientColor * surfaceColor;

    // 2. Diffuse
    float diffuseFactor = max(dot(normal, lightDir), 0);
    float3 diffuse = diffuseFactor * LightColor * surfaceColor;

    // 3. Specular (Blinn-Phong)
    float3 specular = 0;
    if (diffuseFactor > 0)
    {
        float specFactor = pow(max(dot(normal, halfVec), 0), Shininess);
        specular = specFactor * LightColor;
    }

    float3 finalColor = ambient + diffuse + specular;
    return float4(finalColor, texColor.a);
}

// ==========================================
// TECHNIQUE
// ==========================================
technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}