Shader "Custom/Bubble2" {
    Properties
    {
        _Color("Color",Color) = (1,1,1,1)
        _Cube("Cubemap", CUBE) = "" {}
        _MainTex("BubbleTexture",2D) = "white"{}
        _Metallic("Metallic",Range(0,1)) = 1
        _Shininess("Smoothness",Range(0,100)) = 1
        _SpecColor("SpecColor", COLOR) = (0,0,0)
        _TextureAlpha("TextureAlpha",Range(0,1)) = 0.5
        _Alpha("Alpha",Range(0,1)) = 1
    }
        SubShader
        {
            //Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
        ZTest Always
        ZWrite Off

        //Cull Off
            //Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Pass
        {

            Tags{ "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Max
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "AutoLight.cginc"
            #include "GrassUtils.cginc"
            #include "noiseSimplex.cginc"

            sampler2D _MainTex;
            float2 _MainTex_ST;
            float _Metallic;
            float4 _EmissionColor;
            float _TextureAlpha;
            float _Alpha;


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

                
                
                //Object position is i.vertex
                float4 objectPos = mul(_Properties[instanceID].mat, float3(1.99,0,1.25));
                float3 worldPos = mul(unity_ObjectToWorld, objectPos);

                float dist = distance(float3(i.vertex.x, 0, i.vertex.z), float3(0, 0, 0)) / 10;
                float offsetY = sin(_Time.x * 1 * frac(worldPos.x * worldPos.z)) + 1;
                i.vertex.xyz += float3(0, offsetY * 5, 0);
                float noise = snoise(i.vertex.xyz + (_Time.x * worldPos));
                i.vertex.xyz += sin(_Time.x * frac(worldPos.x * worldPos.z) * 100) * (noise) / 10 * i.normal;
                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                float3 world_vertex = mul(unity_ObjectToWorld, pos);

                


                o.vertex = UnityObjectToClipPos(pos);
                o.color = _Properties[instanceID].color;
                o.uv = i.uv;
                o.normal = i.normal;
                o.worldNormal = normalize(mul(_Properties[instanceID].mat, i.normal));
                o.world_vertex = mul(unity_ObjectToWorld, i.vertex);
                o.pos = o.vertex;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i, uint instanceID : SV_InstanceID) : SV_Target
            {
                float3 NdotL = dot(i.worldNormal , normalize(_WorldSpaceCameraPos - i.world_vertex));
                float3 Rim = (1 - NdotL) * _Color;
                NdotL = (NdotL + 1) / 2;
                i.uv = pow(pow((i.uv - 0.5), 2), 0.5) + 0.5;
                float4 Bubble = lerp(0,tex2D(_MainTex, NdotL * _MainTex_ST * i.uv), _TextureAlpha);
                float4 lighting = BlinnPhong(i.worldNormal, i.world_vertex, Bubble);
                //return float4(NdotL.x, NdotL.y, NdotL.z, 1);
                return float4(lighting.rgb * 0.5 * _Color.rgb + 0.5 + Rim, _Alpha);
            }

            ENDCG
        }
    }   
}