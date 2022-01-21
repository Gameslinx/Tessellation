Shader "Custom/InstancedIndirectColor" {
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _BumpMap("Bump Map", 2D) = "white" {}
        _Color("Color", COLOR) = (0,0,0)
        _Cutoff("_Cutoff", Range(0, 1)) = 0.5
        _MaxBrightness("_MaxBrightness", float) = 1
        _WindMap("_WindMap", 2D) = "white" {}
        _WorldSize("_WorldSize", vector) = (0,0,0)
        _WindSpeed("Wind Speed", vector) = (1, 1, 1, 1)
        _WaveSpeed("Wave Speed", float) = 1.0
        _WaveAmp("Wave Amp", float) = 1.0
        _HeightCutoff("Height Cutoff", Range(-1, 1)) = -100
        _HeightFactor("HeightFactor", Range(0, 4)) = 1
        _Shininess("_Shininess", Range(0.001, 100)) = 1
        _SpecColor("_SpecColor", COLOR) = (1,1,1)
        _PlanetOrigin("_PlanetOrigin", vector) = (0,0,0)
        _ShaderOffset("_ShaderOffset", vector) = (0,0,0)
    }
    SubShader
    {
            //Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
        //ZTest Always
        //ZWrite Off
        //Cull Off
            //Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
            //ZTest On
            Tags { "RenderType" = "Opaque"}

        Pass 
        {

            Tags{ "LightMode" = "ForwardBase" }
            //Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "AutoLight.cginc"
            #include "GrassUtils.cginc"
            #include "noiseSimplex.cginc"
             #include "UnityCG.glslinc"
            
            sampler2D _MainTex;
            float2 _MainTex_ST;
            sampler2D _BumpMap;
            float _Cutoff;
            float _MaxBrightness;
            sampler2D _WindMap;
            float4 _WindSpeed;
            float _WaveSpeed;
            float _WaveAmp;
            float _HeightCutoff;
            float _HeightFactor;
            float3 _PlanetOrigin;
            float3 _ShaderOffset;

            struct appdata_t 
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f 
            {
                float4 vertex   : SV_POSITION;
                float4 pos : TEXCOORD3;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 worldNormal : TEXCOORD1;
                float3 world_vertex : TEXCOORD2;

                float3 tangentWorld: TEXCOORD6;
                float3 binormalWorld: TEXCOORD7;


                LIGHTING_COORDS(3, 4)
            };

            struct MeshProperties 
            {
                float4x4 mat;
                float4 color;
            };

            StructuredBuffer<MeshProperties> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;
                float4x4 trueMatrix = _Properties[instanceID].mat;//mul(GetTranslationMatrix(_ShaderOffset), _Properties[instanceID].mat);
                //trueMatrix = mul(unity_ObjectToWorld, trueMatrix);
                //trueMatrix = transpose(trueMatrix);
               // trueMatrix[0].x =  trueMatrix[0].x / 2;
               // trueMatrix[0].y =  trueMatrix[0].y / 2;
               // trueMatrix[0].z =  trueMatrix[0].z / 2;
                //trueMatrix = transpose(trueMatrix);
                float4 pos = mul(trueMatrix, i.vertex) + float4(_ShaderOffset.xyz, 0);
                float3 world_vertex = pos;// mul(trueMatrix, pos);
                //float3 world_vertex = mul(unity_ObjectToWorld, i.vertex);
                //float3 bf = normalize(abs(normalize(world_vertex - _PlanetOrigin)));
                //bf /= dot(bf, (float3)1);
                //float2 xz = world_vertex.zx * bf.y;
                //float2 xy = world_vertex.xy * bf.z;
                //float2 zy = world_vertex.yz * bf.x;
                //
                //
                //
                //float2 samplePosXZ = xz;
                //samplePosXZ += _Time.x * _WindSpeed.xz;
                //samplePosXZ = (samplePosXZ) * _WaveAmp;
                //
                //float2 samplePosXY = xy;
                //samplePosXY += _Time.x * _WindSpeed.xy;
                //samplePosXY = (samplePosXY) * _WaveAmp;
                //
                //float2 samplePosZY = zy;
                //samplePosZY += _Time.x * _WindSpeed.zy;
                //samplePosZY = (samplePosZY) * _WaveAmp;
                //
                //float2 wind = (samplePosXZ + samplePosXY + samplePosZY) / 3;
                //
                //float heightFactor = i.vertex.y > _HeightCutoff;
                //heightFactor = heightFactor * pow(i.vertex.y, _HeightFactor);
                //if (i.vertex.y < 0)
                //{
                //    heightFactor = 0;
                //}
                //
                //float2 windSample = -tex2Dlod(_WindMap, float4(wind, 0, 0));
                //
                ////wind = -windSample;
                //
                //float3 positionOffset = mul(unity_ObjectToWorld, float3(windSample.x, 0, windSample.y));//mul(float3(windSample.x, 0, windSample.y), unity_ObjectToWorld);
                //
                //pos.xyz += sin(_WaveSpeed * positionOffset) * heightFactor;

                o.vertex = UnityObjectToClipPos(pos);
                o.color = _Properties[instanceID].color;
                o.uv = i.uv;
                o.normal = i.normal;
                o.worldNormal = normalize(mul(trueMatrix, i.normal));
                o.world_vertex = mul(trueMatrix, i.vertex);
                o.pos = o.vertex;
                o.tangentWorld = normalize(mul(trueMatrix, i.tangent).xyz);
                o.binormalWorld = normalize(cross(o.worldNormal, o.tangentWorld));
                //o.wind = wind;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed3 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv * _MainTex_ST) * i.color * _Color;
                float3 normalMap = UnpackNormal(tex2D(_BumpMap, i.uv));
                float3x3 TBN = float3x3(normalize(i.tangentWorld), normalize(i.binormalWorld), normalize(i.worldNormal));
                TBN = transpose(TBN);
                float3 worldNormal = mul(TBN, normalMap);
                i.worldNormal = normalize(worldNormal);

                float4 color = BlinnPhong(i.worldNormal, i.world_vertex, col);
                float atten = saturate(LIGHT_ATTENUATION(i) + UNITY_LIGHTMODEL_AMBIENT.rgb);
                color.rgb *= atten;
                //return float4(normalMap, 1);
                return color.rgb;
            }

            ENDCG
        }
        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#include "UnityCG.cginc"
#include "GrassUtils.cginc"
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 pos : TEXCOORD3;
                float3 normal : NORMAL;
                float3 worldNormal : TEXCOORD1;
                float3 world_vertex : TEXCOORD2;
                float2 uv : TEXCOORD0;
            };
            struct MeshProperties
            {
                float4x4 mat;
                float4 color;
            };
            StructuredBuffer<MeshProperties> _Properties;
            sampler2D _WindMap;
            float4 _WindSpeed;
            float _WaveSpeed;
            float _WaveAmp;
            float _HeightCutoff;
            float _HeightFactor;
            float3 _PlanetOrigin;
            float _Cutoff;
            sampler2D _MainTex;
            float2 _MainTex_ST;
            float3 _ShaderOffset;
            v2f vert(appdata_t v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float4x4 trueMatrix = mul(GetTranslationMatrix(_ShaderOffset), _Properties[instanceID].mat);
                float4 pos = mul(trueMatrix, v.vertex);
                float3 world_vertex = pos;
                //float3 bf = normalize(abs(normalize(world_vertex - _PlanetOrigin)));
                //bf /= dot(bf, (float3)1);
                //float2 xz = world_vertex.zx * bf.y;
                //float2 xy = world_vertex.xy * bf.z;
                //float2 zy = world_vertex.yz * bf.x;
                //
                //
                //
                //float2 samplePosXZ = xz;
                //samplePosXZ += _Time.x * _WindSpeed.xz;
                //samplePosXZ = (samplePosXZ)*_WaveAmp;
                //
                //float2 samplePosXY = xy;
                //samplePosXY += _Time.x * _WindSpeed.xy;
                //samplePosXY = (samplePosXY)*_WaveAmp;
                //
                //float2 samplePosZY = zy;
                //samplePosZY += _Time.x * _WindSpeed.zy;
                //samplePosZY = (samplePosZY)*_WaveAmp;
                //
                //float2 wind = (samplePosXZ + samplePosXY + samplePosZY) / 3;
                //
                //float heightFactor = v.vertex.y > _HeightCutoff;
                //heightFactor = heightFactor * pow(v.vertex.y + 0.05, _HeightFactor);
                //if (v.vertex.y < 0)
                //{
                //    heightFactor = 0;
                //}
                //
                //float2 windSample = -tex2Dlod(_WindMap, float4(wind, 0, 0));
                //
                //float3 positionOffset = mul(float3(windSample.x, 0, windSample.y), unity_ObjectToWorld);
                //
                //pos.xyz += sin(_WaveSpeed * positionOffset) * heightFactor;
        
                o.vertex = UnityObjectToClipPos(pos);
                o.normal = v.normal;
                o.worldNormal = normalize(mul(trueMatrix, v.vertex));
                o.world_vertex = mul(trueMatrix, v.vertex);
                o.pos = o.vertex;
                o.pos = UnityApplyLinearShadowBias(o.pos);
                o.uv = v.uv;
                //TRANSFER_SHADOW_CASTER(o)
                return o;
                //vertex = mul(unity_ObjectToClipPos, vertex);
                //normal = mul(unity_ObjectToWorld, normal);
               //vertex = mul(UNITY_MATRIX_VP, mul(_Properties[instanceID].mat, vertex));
               //normal = mul(UNITY_MATRIX_VP, mul(_Properties[instanceID].mat, normal));
            }
        
            float3 frag(v2f i) : SV_Target
            {
                //fixed4 texcol = tex2D(_MainTex, i.uv);
                //if (_Cutoff > texcol.a)
                //{
                //    discard;
                //}
                return 0;
                //fixed4 texcol = tex2D(_MainTex, i.uv);
                //clip(texcol.a * _Color.a - _Cutoff);
                //
                //SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    //Fallback "Transparent/Cutout/Diffuse"
}