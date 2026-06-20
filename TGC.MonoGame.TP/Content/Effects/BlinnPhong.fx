// =============================================================
// BlinnPhong.fx — Iluminacion Blinn-Phong + deformacion por impacto
// Shader Model 3.0
// =============================================================
//orugas
float2 TextureOffset;

// ---------- Transformaciones ----------
float4x4 World;
float4x4 View;
float4x4 Projection;

// ---------- Iluminacion Blinn-Phong ----------
float3 LightDirection; // direccion HACIA la luz (normalizada)
float3 LightColor;
float3 AmbientColor;
float3 EyePosition;
float Shininess = 32.0f;

// ---------- Textura ----------
float3 DiffuseColor = float3(1.0, 1.0, 1.0);

texture ModelTexture;
sampler2D TextureSampler = sampler_state
{
    Texture = <ModelTexture>;
    // "Wrap" es la clave: hace que la textura sea un ciclo infinito
    AddressU = Wrap; 
    AddressV = Wrap; 
    MinFilter = Linear;
    MagFilter = Linear;
};

// ---------- Deformacion por impacto ----------
float3 ImpactPointWorld; // punto de impacto en coordenadas del mundo
float ImpactRadius; // radio de deformacion
float ImpactDepth; // profundidad maxima del hundimiento
int HasImpact; // 1 = hay impacto activo, 0 = no
int IsDeformable; // 1 = este mesh se puede deformar, 0 = no

// =============================================================
// Estructuras
// =============================================================
struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float3 WorldNormal : TEXCOORD2;
};

// =============================================================
// Vertex Shader
// =============================================================
VSOutput VS(VSInput input)
{
    VSOutput output;

    // Posicion y normal en espacio mundo
    float4 worldPos = mul(input.Position, World);
    float3 worldNormal = normalize(mul(input.Normal, (float3x3) World));

    // ---------- Deformacion ----------
    if (HasImpact && IsDeformable)
    {
        float3 delta = worldPos.xyz - ImpactPointWorld;
        float dist = length(delta);

        if (dist < ImpactRadius)
        {
            // Factor 1 en el centro, 0 en el borde (suavizado cuadratico)
            float t = 1.0f - (dist / ImpactRadius);
            float factor = t * t;

            // Hundir el vertice a lo largo de su normal original
            worldPos.xyz -= worldNormal * ImpactDepth * factor;

            // Recalcular normal: inclinarla hacia afuera del crater
            float3 craterDir = normalize(delta + float3(0.001f, 0.001f, 0.001f));
            worldNormal = normalize(worldNormal + craterDir * factor * 1.5f);
        }
    }

    // Proyectar a pantalla
    float4 viewPos = mul(worldPos, View);
    output.Position = mul(viewPos, Projection);

    // ---------- APLICAR OFFSET DE ORUGAS ----------
    // Sumamos el TextureOffset a la coordenada original de la malla
    output.TexCoord = input.TextureCoordinate + TextureOffset;
    
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = worldNormal;

    return output;
}

// =============================================================
// Pixel Shader — Blinn-Phong
// =============================================================
float4 PS(VSOutput input) : COLOR0
{
    // Muestrear textura
    float4 texColor = tex2D(TextureSampler, input.TexCoord);
    float3 baseColor = texColor.rgb * DiffuseColor;

    // Normal interpolada (renormalizar)
    float3 N = normalize(input.WorldNormal);

    // Vector hacia la luz
    float3 L = normalize(LightDirection);

    // Vector hacia la camara
    float3 V = normalize(EyePosition - input.WorldPos);

    // Half vector (Blinn-Phong)
    float3 H = normalize(L + V);

    // Componente difusa
    float NdotL = max(dot(N, L), 0.0f);
    float3 diffuse = LightColor * NdotL;

    // Componente especular
    float NdotH = max(dot(N, H), 0.0f);
    float3 specular = LightColor * pow(NdotH, Shininess);

    // Color final = ambient + diffuse + specular, multiplicado por textura
    float3 lighting = AmbientColor + diffuse;
    float3 finalColor = baseColor * lighting + specular * 0.3f;

    return float4(saturate(finalColor), texColor.a);
}

// =============================================================
// Technique
// =============================================================
technique BlinnPhong
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}