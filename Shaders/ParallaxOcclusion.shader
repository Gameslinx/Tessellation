

Shader "Custom/ParallaxOcclusion"
{
    Properties
    {
        _SurfaceTexture("_SurfaceTexture", 2D) = "white" {}
        _SurfaceVarianceTexture("_SurfaceVarianceTexture", 2D) = "white" {}
        _SurfaceVarianceTexturePow("_SurfaceVarianceTexturePow", Range(0, 10)) = 1
        _SurfaceVarianceTextureScale("_SurfaceVarianceTextureScale", Range(0, 2)) = 1
        _SteepTex("_SteepTex", 2D) = "white" {}
        _SteepPower("_SteepPower", Range(0.01, 10)) = 1
        _Strength("_Strength", Range(0, 100)) = 1
        [NoScaleOffset] _BumpMap("_BumpMap", 2D) = "bump" {}
        [NoScaleOffset] _SurfaceVarianceBumpMap("_SurfaceVarianceBumpMap", 2D) = "bump" {}
        _ParallaxMap("_ParallaxMap", 2D) = "white" {}
        _ParallaxMapMulti("_ParallaxMapMulti", 2D) = "white" {}
        _Parallax("_Parallax", Range(0, 1)) = 0.05
        _ParallaxRange("_ParallaxRange", Range(0, 2000)) = 80
        _ParallaxMinSamples("_ParallaxMinSamples", Range(1, 100)) = 1
        _ParallaxMaxSamples("_ParallaxMaxSamples", Range(1, 400)) = 100
        _PlanetOrigin("_PlanetOrigin", vector) = (0,0,0)
        _NoiseTex("_NoiseTex", 2D) = "white" {}
        _Metallic("_Metallic (Specular)", Range(0, 2)) = 0.308
        _MetallicTint("_MetallicTint", COLOR) = (1,1,1)
        _LightPos("_LightPos", vector) = (0, 0, 0)
    }
     SubShader
    {
        Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"
           #include <ParallaxOcclusion.cginc>
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord : TEXCOORD4;
                float sampleRatio : TEXCOORD7;
                
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD4;
                half3 tspace0 : TEXCOORD1;
                half3 tspace1 : TEXCOORD2;
                half3 tspace2 : TEXCOORD3;
                float cameraDist : TEXCOORD5;
                float3 eye : TEXCOORD6;
                float3 reverseEye : TEXCOORD9;
                float sampleRatio : TEXCOORD7;
                float reverseSampleRatio : TEXCOORD10;
                float2 texcoord : TEXCOORD0;
                float3 normal : TEXCOORD8;
                float4 tangent : TEXCOORD11;
            };

            sampler2D _SurfaceTexture;
            sampler2D _SteepTex;
            sampler2D _SurfaceVarianceTexture;
            float4 _SurfaceTexture_ST;
            float4 _SteepTex_ST;
            float _Strength;
            float _SteepPower;
            sampler2D _BumpMap;
            sampler2D _SurfaceVarianceBumpMap;
            float _Parallax;
            float _SurfaceVarianceTexturePow;
            float4 _SurfaceVarianceTexture_ST;
            float3 _PlanetOrigin;
            sampler2D _ParallaxMap;
            sampler2D _ParallaxMapMulti;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _SurfaceVarianceTextureScale;
            int _ParallaxMinSamples;
            int _ParallaxMaxSamples;
            float _Metallic;
            float _ParallaxRange;
            float3 _LightPos;
            float3 _MetallicTint;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;    //Use this when calculating slope
                o.texcoord = v.texcoord;


                //PARALLAX VERT CALCULATION
                v.tangent = (v.tangent);
                parallax_vert(v.vertex, v.normal, v.tangent, o.eye, o.sampleRatio);                         //Positive X and Z
                reverse_parallax_vert(v.vertex, v.normal, -v.tangent, o.reverseEye, o.reverseSampleRatio);  //Negative X and Z have reversed tangents

                half3 wNormal = UnityObjectToWorldNormal(v.normal);
                

                half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);         
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                o.normal = mul(unity_ObjectToWorld, v.normal);


                o.cameraDist = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));
                o.tangent = v.tangent;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                //Slope calculation relative to the planet origin (mountain slopes). This value determines the blending of textures
                float slope = abs(dot(normalize(i.worldPos - _PlanetOrigin), normalize(i.normal)));


                ///////////////////////////////////////////////
                //////////////Texture Zoom Levels//////////////
                ///////////////////////////////////////////////
                float2 surfaceTexcoord = (i.worldPos.xz * (_SurfaceTexture_ST.xy) + _SurfaceTexture_ST.zw);
                float ZoomLevel = ((0.8 * pow(i.cameraDist, 0.18) - 0.5));
                ZoomLevel = clamp(ZoomLevel, 1, 10);
                ZoomLevel = 1;  //set ZoomLevel to 1 while debugging parallax effect
                int ClampedZoomLevel = floor(ZoomLevel);
                if (ClampedZoomLevel >= 10)
                {
                    ClampedZoomLevel = 10;
                    ZoomLevel = 10;
                }
                float percentage = (ZoomLevel - ClampedZoomLevel);
                float uvDistortion = pow(ClampedZoomLevel, 3);
                float nextUVDist = pow((ClampedZoomLevel + 1), 3);


                //WORLD-SPACE NORMAL
                half3 vertexNormal = abs(normalize(half3(i.tspace0.z, i.tspace1.z, i.tspace2.z)));

                //TRIPLANAR UV COORDS//
                float2 uvX = (i.worldPos.zy * _SurfaceTexture_ST.xy + _SurfaceTexture_ST.zw);
                float2 uvY = (i.worldPos.xz * (_SurfaceTexture_ST.xy) + _SurfaceTexture_ST.zw);
                float2 uvZ = (i.worldPos.xy * _SurfaceTexture_ST.xy + _SurfaceTexture_ST.zw);

                /////////////////////////////////
                ///////PARALLAX CALCULATION//////
                /////////////////////////////////

                float2 offsetSurfX = 0;
                float2 offsetSurfZ = 0;
                float2 offsetSurfY = 0;
                float2 offsetSurfX2 = 0;
                float2 offsetSurfZ2 = 0;
                float2 offsetSurfY2 = 0;
                float parallaxCameraRangeIntensity = (i.cameraDist / _ParallaxRange);


                if (parallaxCameraRangeIntensity > 1)
                {
                    parallaxCameraRangeIntensity = 1;
                }
                if (parallaxCameraRangeIntensity < 0)
                {
                    parallaxCameraRangeIntensity = 0;
                }
                parallaxCameraRangeIntensity = 1 - parallaxCameraRangeIntensity;
                
                _Parallax = parallaxCameraRangeIntensity * _Parallax;
                
                if (i.cameraDist < _ParallaxRange + 10)     //Shader etiquette 101 is to use minimal conditional statements but this massively improves performance
                {
                    if (i.worldPos.x - _PlanetOrigin.x >= 0)
                    {
                        offsetSurfX = parallax_offset(_Parallax, i.eye, i.sampleRatio, uvX, _ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples);  //POSITIVE X
                    }
                    else
                    {
                        offsetSurfX = parallax_offset(_Parallax, i.reverseEye, i.reverseSampleRatio, uvX, _ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples);    //NEGATIVE X
                    }
                    if (i.worldPos.z <= 0)
                    {
                        offsetSurfZ = parallax_offset(_Parallax, i.eye, i.sampleRatio, uvZ, _ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples);  //POSITIVE Z
                    }
                    else
                    {
                        offsetSurfZ = parallax_offset(_Parallax, i.reverseEye, i.reverseSampleRatio, uvZ, _ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples);    //NEGATIVE Z
                    }
                    if (i.worldPos.y - _PlanetOrigin.y >= 0)
                    {
                        offsetSurfY = parallax_offset(-_Parallax, i.reverseEye, i.reverseSampleRatio, uvY, _ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples);   //POSITIVE Y
                    }
                    else
                    {
                        offsetSurfY = parallax_offset(-_Parallax, i.eye, i.sampleRatio, uvY, _ParallaxMap, _ParallaxMinSamples, _ParallaxMaxSamples);     //NEGATIVE Y
                    }
                }
                



                half3 triblend = pow(abs(vertexNormal), _Strength * 1.4); //Triblend determines blending of triplanar textures
                triblend /= max(dot(triblend, half3(1, 1, 1)), 0.000000);


                float currentuvX = uvX / uvDistortion;
                float currentuvY = uvY / uvDistortion;
                float currentuvZ = uvZ / uvDistortion;
                float nextuvX = uvX / nextUVDist;
                float nextuvY = uvY / nextUVDist;
                float nextuvZ = uvZ / nextUVDist;
                
                slope = pow(slope, _SteepPower);
                

                //Noisemap tiling
                fixed4 multiTexBlendX = tex2D(_NoiseTex, uvX * _SurfaceVarianceTextureScale);
                fixed4 multiTexBlendY= tex2D(_NoiseTex, uvY * _SurfaceVarianceTextureScale);
                fixed4 multiTexBlendZ = tex2D(_NoiseTex, uvZ * _SurfaceVarianceTextureScale);
                fixed4 multiTexBlend = multiTexBlendX * triblend.x + multiTexBlendY * triblend.y + multiTexBlendZ * triblend.z;
                float multiTexBlendPow = pow(multiTexBlend, _SurfaceVarianceTexturePow);

                fixed4 colX = tex2D(_SurfaceTexture, uvX / uvDistortion + offsetSurfX);
                fixed4 colY = tex2D(_SurfaceTexture, uvY  / uvDistortion + offsetSurfY);   //surface tex with parallax offset
                fixed4 colZ = tex2D(_SurfaceTexture, uvZ / uvDistortion + offsetSurfZ);    //UvDistortion refers to the texture zoom-out levels and does not affect parallax effect

                fixed4 colX2 = tex2D(_SurfaceTexture, uvX / nextUVDist);
                fixed4 colY2 = tex2D(_SurfaceTexture, uvY / nextUVDist);    //surface tex zoomed out
                fixed4 colZ2 = tex2D(_SurfaceTexture, uvZ / nextUVDist);
                
                fixed4 colXSteep = tex2D(_SteepTex, uvX / uvDistortion);
                fixed4 colYSteep = tex2D(_SteepTex, uvY / uvDistortion);    //steep tex
                fixed4 colZSteep = tex2D(_SteepTex, uvZ / uvDistortion);

                fixed4 colXSteep2 = tex2D(_SteepTex, uvX / nextUVDist);
                fixed4 colYSteep2 = tex2D(_SteepTex, uvY / nextUVDist);    //steep tex
                fixed4 colZSteep2 = tex2D(_SteepTex, uvZ / nextUVDist);

               



                fixed4 colXMulti = tex2D(_SurfaceVarianceTexture, (uvX * _SurfaceVarianceTexture_ST) / uvDistortion);
                fixed4 colYMulti = tex2D(_SurfaceVarianceTexture, (uvY * _SurfaceVarianceTexture_ST) / uvDistortion);   //surface variance tex
                fixed4 colZMulti = tex2D(_SurfaceVarianceTexture, (uvZ * _SurfaceVarianceTexture_ST) / uvDistortion);

                fixed4 actualColXSteep = lerp(colXSteep, colXSteep2, percentage);
                fixed4 actualColYSteep = lerp(colYSteep, colYSteep2, percentage);   //Blend slope texture with the zoomed out version
                fixed4 actualColZSteep = lerp(colZSteep, colZSteep2, percentage);

                fixed4 actualColX = lerp(colX, colX2, percentage);
                fixed4 actualColY = lerp(colY, colY2, percentage);  //UV distortion blending
                fixed4 actualColZ = lerp(colZ, colZ2, percentage);

                fixed4 blendColX = lerp(actualColX, colXMulti, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                fixed4 blendColY = lerp(actualColY, colYMulti, pow(multiTexBlend, _SurfaceVarianceTexturePow)); //multi-tex blending
                fixed4 blendColZ = lerp(actualColZ, colZMulti, pow(multiTexBlend, _SurfaceVarianceTexturePow));

                fixed4 finalColX = lerp(blendColX, actualColXSteep, 1 - slope);
                fixed4 finalColY = lerp(blendColY, actualColYSteep, 1 - slope); //Final albedo color before lighting is applied
                fixed4 finalColZ = lerp(blendColZ, actualColZSteep, 1 - slope);
                

                finalColX.a = lerp(actualColX.a, colXMulti.a, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                finalColY.a = lerp(actualColY.a, colYMulti.a, pow(multiTexBlend, _SurfaceVarianceTexturePow));  //Lerp the alpha channel for specular lighting
                finalColZ.a = lerp(actualColZ.a, colZMulti.a, pow(multiTexBlend, _SurfaceVarianceTexturePow));

                fixed4 col = finalColX * triblend.x + finalColY * triblend.y + finalColZ * triblend.z;
                //return offset.x * 4;
                // tangent space normal map
                half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX + offsetSurfX));
                half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY + offsetSurfY));      //Triplanar normal mapping
                half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ + offsetSurfZ));

                half3 tnormalSVX = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, uvX + offsetSurfX));        //Second surface texture triplanar normal mapping
                half3 tnormalSVY = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, uvY + offsetSurfY));
                half3 tnormalSVZ = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, uvZ + offsetSurfZ));

                half3 tnormal = tnormalX * triblend.x + tnormalY * triblend.y + tnormalZ * triblend.z;
                half3 tnormalSV = tnormalSVX * triblend.x + tnormalSVY * triblend.y + tnormalSVZ * triblend.z;
                tnormal = lerp(tnormal, tnormalSV, pow(multiTexBlend, _SurfaceVarianceTexturePow));

                half3 worldNormal = normalize(half3(
                    dot(i.tspace0, tnormal),
                    dot(i.tspace1, tnormal),
                    dot(i.tspace2, tnormal)
                    ));
                

                //////////////////
                /////LIGHTING/////
                //////////////////
                half ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                half3 ambient = ShadeSH9(half4(worldNormal, 1));
                half3 lighting = _LightColor0.rgb * ndotl + ambient;
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfVector = normalize(lightDir + viewDir);
                float3 specular = _LightColor0 * pow(DotClamped(halfVector, i.normal), _Metallic * 100);


                specular *= _MetallicTint;


                return fixed4(col.rgb + (specular.rgb * col.a), 1);
                
            }
            
            ENDCG
        }
    }
}