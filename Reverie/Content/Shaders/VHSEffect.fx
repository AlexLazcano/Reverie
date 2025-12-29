#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

// Parameters
float Time;
float NoiseAmount = 0.05;
float ScanlineIntensity = 0.1;
float ChromaticAberration = 0.003;
float VignetteStrength = 0.3;

texture ScreenTexture;
sampler2D textureSampler = sampler_state
{
    Texture = <ScreenTexture>;
};

// Random function for noise
float rand(float2 co)
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR
{
    // Chromatic aberration (color separation)
    float2 offset = float2(ChromaticAberration, 0);
    float r = tex2D(textureSampler, texCoord + offset).r;
    float g = tex2D(textureSampler, texCoord).g;
    float b = tex2D(textureSampler, texCoord - offset).b;
    float4 col = float4(r, g, b, 1.0);
    
    // Scanlines
    float scanline = sin(texCoord.y * 800.0) * ScanlineIntensity;
    col.rgb -= scanline;
    
    // Film grain
    float noise = rand(texCoord + Time) * NoiseAmount;
    col.rgb += noise;
    
    // Vignette (darker edges)
    float2 center = texCoord - 0.5;
    float vignette = 1.0 - dot(center, center) * VignetteStrength;
    col.rgb *= vignette;
    
    // VHS tracking issues (horizontal displacement)
    float trackingNoise = rand(float2(texCoord.y * 10.0, Time * 0.5));
    if (trackingNoise > 0.95)
    {
        float2 displaced = texCoord + float2(rand(float2(texCoord.y, Time)) * 0.05, 0);
        col = tex2D(textureSampler, displaced);
    }
    
    // Color degradation (slight desaturation with color shift)
    float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
    col.rgb = lerp(col.rgb, float3(gray, gray, gray), 0.1);
    col.rgb *= float3(1.0, 0.95, 1.05); // Slight blue/magenta tint
    
    return col;
}

technique VHS
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}