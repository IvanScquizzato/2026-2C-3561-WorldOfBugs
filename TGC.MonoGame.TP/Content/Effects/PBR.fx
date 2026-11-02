#define PI 3.14159265359f
float Roughness;
float K;
float3 F0;

float3 CameraPosition;
float3 SunlightDirection = float3(-1, -1, -1);
float LightIntensity;

float3 BDRF(float3 albedo, float3 omega0, float3 omegai, float3 normal)
{
    float3 H = normalize(omega0 + omegai + float3(0.0001f, 0.0f, 0.0f));

    float NdotH = max(dot(normal, H), 0.0001f);
    float NdotV = max(dot(normal, omega0), 0.0001f);
    float NdotL = max(dot(normal, omegai), 0.0001f);
    float HdotV = max(dot(H, omega0), 0.0001f);

    float safeRoughness = max(Roughness, 0.05f); 
    float alphaSq = safeRoughness * safeRoughness * safeRoughness * safeRoughness; 
    
    float denom = (NdotH * NdotH) * (alphaSq - 1.0f) + 1.0f;
    denom = max(denom, 0.0001f); 
    float NormalDistribution = alphaSq / (PI * denom * denom);

    float baseFresnel = saturate(1.0f - HdotV);
    float baseFresnel2 = baseFresnel * baseFresnel;
    float FresnelQuintic = baseFresnel2 * baseFresnel2 * baseFresnel; 
    float3 Fresnel = F0 + (1.0f - F0) * FresnelQuintic;
    
    float3 Kd = float3(1.0f, 1.0f, 1.0f) - Fresnel;

    float safeK = max(K, 0.01f);
    float Gv = NdotV / (NdotV * (1.0f - safeK) + safeK);
    float Gl = NdotL / (NdotL * (1.0f - safeK) + safeK);
    float Geometry = Gv * Gl;
    
    float denominator = max(4.0f * NdotV * NdotL, 0.001f);
    float3 CookTorrance = (NormalDistribution * Fresnel * Geometry) / denominator;
    
    float3 DiffuseComponent = (Kd * albedo.xyz / PI); 
    float3 AmbientLight = albedo.xyz * 0.6f; 

    return (DiffuseComponent + CookTorrance) * NdotL * LightIntensity + AmbientLight;
}