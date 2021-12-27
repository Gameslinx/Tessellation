Shader "Custom/InstancedIndirectColor" {
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _BumpMap("Bump Map", 2D) = "white" {}
        _Color("Color", COLOR) = (0,0,0)
        _Cutoff("_Cutoff", float) = 0.5
    }
    SubShader
    {
            //Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
        //ZTest Always
        //ZWrite Off
        //Cull Off
            Tags {"RenderType" = "Opaque" }
        Pass 
        {
            Tags{ "LightMode" = "ForwardBase" }
            //Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "AutoLight.cginc"
            #include "UnityCG.cginc"

            float4 _Color;
            sampler2D _MainTex;
            float2 _MainTex_ST;
            sampler2D _BumpMap;
            float _Cutoff;
            struct appdata_t 
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
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

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                o.world_vertex = mul(unity_ObjectToWorld, pos);
                o.vertex = UnityObjectToClipPos(pos);
                o.color = _Properties[instanceID].color;
                o.uv = i.uv;
                o.normal = i.normal;
                o.worldNormal = mul(mul(_Properties[instanceID].mat, i.normal), unity_ObjectToWorld);
                o.pos = o.vertex;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed3 frag(v2f i) : SV_Target{
                //return float4(i.worldNormal, 1);
                float3 col = tex2D(_MainTex, i.uv * _MainTex_ST).rgb * i.color;
                float ndotl = saturate(dot(i.worldNormal, _WorldSpaceLightPos0));
                //return float4(ndotl, ndotl, ndotl, 1);
                float ambient = UNITY_LIGHTMODEL_AMBIENT;
                ndotl = saturate(ndotl + ambient);
                float3 finalCol = col * ndotl;
            
                float atten = LIGHT_ATTENUATION(i);
                finalCol *= (atten + ambient);
                //return float3(atten, atten, atten);
                return finalCol;
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
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 pos : TEXCOORD3;
                float3 normal : NORMAL;
                float3 worldNormal : TEXCOORD1;
                float3 world_vertex : TEXCOORD2;
            };
            struct MeshProperties
            {
                float4x4 mat;
                float4 color;
            };
            StructuredBuffer<MeshProperties> _Properties;
            
            v2f vert(appdata_t i, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                o.world_vertex = mul(unity_ObjectToWorld, pos);
                o.vertex = UnityObjectToClipPos(pos);
                o.normal = i.normal;
                o.worldNormal = mul(mul(_Properties[instanceID].mat, i.normal), unity_ObjectToWorld);
                o.pos = o.vertex;
                return o;
                //vertex = mul(unity_ObjectToClipPos, vertex);
                //normal = mul(unity_ObjectToWorld, normal);
               //vertex = mul(UNITY_MATRIX_VP, mul(_Properties[instanceID].mat, vertex));
               //normal = mul(UNITY_MATRIX_VP, mul(_Properties[instanceID].mat, normal));
            }
        
            float4 frag(float4 vertex:POSITION, float2 uv : TEXCOORD0) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
    //Fallback "VertexLit"
}