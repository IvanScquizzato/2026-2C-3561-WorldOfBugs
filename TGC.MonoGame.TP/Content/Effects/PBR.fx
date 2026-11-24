#define PI 3.14159265359f
float Roughness;
float K;
float3 F0;

float3 CameraPosition;
float3 SunlightDirection;
float3 SunlightColor;
float LightIntensity;
float AmbientIntensity;

#include "PointShadowSampling.fx"

#define MAX_POINT_LIGHTS 4

float4 PointLightPositionRadius[MAX_POINT_LIGHTS];
float4 PointLightColorIntensity[MAX_POINT_LIGHTS];
int PointLightCount;

float3 BDRF(float3 albedo, float3 omega0, float3 omegai, float3 normal, float shadowVisibility)
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
    float3 AmbientLight = albedo.xyz * AmbientIntensity;

    return (DiffuseComponent + CookTorrance) * NdotL * LightIntensity * SunlightColor * shadowVisibility + AmbientLight;
}

float3 CalculatePointLights(float3 albedo, float3 worldPosition, float3 normal)
{
    float3 lighting = float3(0.0f, 0.0f, 0.0f);

    [unroll]
    for (int i = 0; i < MAX_POINT_LIGHTS; i++)
    {
        if (i < PointLightCount)
        {
            float4 positionRadius = PointLightPositionRadius[i];
            float4 colorIntensity = PointLightColorIntensity[i];

            float3 toLight = positionRadius.xyz - worldPosition;
            float distanceToLight = length(toLight);
            float3 lightDirection = toLight / max(distanceToLight, 0.001f);

            float facing = saturate(dot(normal, lightDirection));
            float radius = max(positionRadius.w, 0.001f);
            float falloff = saturate(1.0f - distanceToLight / radius);

            if (facing > 0.0f && falloff > 0.0f)
            {
                float visibility = PointShadowVisibility(i, -toLight, distanceToLight, radius, facing);
                lighting += colorIntensity.rgb
                    * (colorIntensity.w * facing * falloff * falloff * visibility);
            }
        }
    }

    return albedo * lighting;
}
