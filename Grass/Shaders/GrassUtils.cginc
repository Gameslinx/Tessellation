#include "UnityCG.cginc"
#include "Lighting.cginc"
float4 _Color;
float _Shininess;
float4 BlinnPhong(float3 normal, float3 pos, float4 diffuseCol)
{
    half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
    half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - pos);
    half3 halfDir = normalize(lightDir + viewDir);

    // Dot
    half NdotL = saturate(dot(normal, lightDir));
    half NdotH = saturate(dot(normal, halfDir));

    // Color
    fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * diffuseCol.rgb;
    fixed3 diffuse = _LightColor0.rgb * diffuseCol.rgb * NdotL;
    fixed3 specular = _LightColor0.rgb * _SpecColor.rgb * pow(NdotH, _Shininess);
    specular *= diffuseCol.a;
    fixed4 color = fixed4(ambient + diffuse + specular, 1.0);

    return color;
}