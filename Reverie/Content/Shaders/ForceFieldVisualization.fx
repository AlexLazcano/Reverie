#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

sampler TextureSampler : register(s0);

float Time;
float Intensity = 0.5;
float2 ScreenSize;

// Improved hash for gradients
float2 hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453) * 2.0 - 1.0;
}

// Gradient noise (MUCH smoother)
float noise(float2 p, float z)
{
    p += float2(0.0, z);
    
    float2 i = floor(p);
    float2 f = frac(p);
    
    // Quintic interpolation
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    
    // Gradients at corners
    float2 ga = hash2(i + float2(0, 0));
    float2 gb = hash2(i + float2(1, 0));
    float2 gc = hash2(i + float2(0, 1));
    float2 gd = hash2(i + float2(1, 1));
    
    // Dot products
    float va = dot(ga, f - float2(0, 0));
    float vb = dot(gb, f - float2(1, 0));
    float vc = dot(gc, f - float2(0, 1));
    float vd = dot(gd, f - float2(1, 1));
    
    // Interpolate
    return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y) * 0.5 + 0.5;
}

// FBM with smooth noise
float fbm(float2 p, float time)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for(int i = 0; i < 6; i++)
    {
        value += amplitude * noise(p * frequency, time * 0.5);
        frequency *= 2.0;
        amplitude *= 0.5;
    }
    
    return value;
}

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 sceneColor = tex2D(TextureSampler, texCoord);
    float2 uv = texCoord;
    
    // === NEBULA CLOUDS ===
    
    float nebula1 = fbm(uv * 3.0 + float2(Time * 0.02, 0.0), Time * 0.1);
    float nebula2 = fbm(uv * 5.0 + float2(0.0, Time * 0.03), Time * 0.15);
    float nebula3 = fbm(uv * 2.0 - float2(Time * 0.01, Time * 0.01), Time * 0.2);
    
    float nebulaDensity = (nebula1 + nebula2 * 0.5 + nebula3 * 0.3) / 1.8;
    nebulaDensity = smoothstep(0.2, 0.8, nebulaDensity);
    
    // === NEBULA COLORS ===
    
    float colorNoise = fbm(uv * 2.5 + float2(Time * 0.015, -Time * 0.015), Time * 0.1);
    
    float3 nebulaColor1 = float3(0.2, 0.4, 0.9);  // Deep blue
    float3 nebulaColor2 = float3(0.6, 0.3, 0.9);  // Purple
    float3 nebulaColor3 = float3(0.3, 0.8, 1.0);  // Cyan
    float3 nebulaColor4 = float3(0.8, 0.5, 1.0);  // Magenta
    
    float3 nebulaColor;
    if(colorNoise < 0.33)
        nebulaColor = lerp(nebulaColor1, nebulaColor2, colorNoise * 3.0);
    else if(colorNoise < 0.66)
        nebulaColor = lerp(nebulaColor2, nebulaColor3, (colorNoise - 0.33) * 3.0);
    else
        nebulaColor = lerp(nebulaColor3, nebulaColor4, (colorNoise - 0.66) * 3.0);
    
    // === COSMIC DUST ===
    
    float dust = fbm(uv * 7.0 + float2(Time * 0.04, -Time * 0.03), Time * 0.2);
    dust = smoothstep(0.35, 0.65, dust) * 0.12;
    
    // === COMBINE ===
    
    float3 galaxy = nebulaColor * nebulaDensity * Intensity;
    galaxy += dust * nebulaColor * 0.4;
    
    sceneColor.rgb += galaxy;
    
    return sceneColor;
}

technique GalaxyShader
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}