// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/ParallaxOcclusion"
{
    Properties
    {
        _SurfaceTexture("_SurfaceTexture", 2D) = "white" {}
        _SurfaceTextureMid("_SurfaceTextureMid", 2D) = "white" {}
        _SurfaceTextureHigh("_SurfaceTextureHigh", 2D) = "white" {}
        _SurfaceVarianceTexture("_SurfaceVarianceTexture", 2D) = "white" {}
        _SurfaceVarianceTexturePow("_SurfaceVarianceTexturePow", Range(0, 10)) = 1
        _SurfaceVarianceTextureScale("_SurfaceVarianceTextureScale", Range(0, 2)) = 1
        _SteepTex("_SteepTex", 2D) = "white" {}
        _SteepPower("_SteepPower", Range(0.01, 10)) = 1
        _Strength("_Strength", Range(0, 100)) = 100
        [NoScaleOffset] _BumpMap("_BumpMap", 2D) = "bump" {}
        [NoScaleOffset] _BumpMapMid("_BumpMapMid", 2D) = "bump" {}
        [NoScaleOffset] _BumpMapHigh("_BumpMapHigh", 2D) = "bump" {}
        [NoScaleOffset] _BumpMapSteep("_BumpMapSteep", 2D) = "bump" {}

        [NoScaleOffset] _SurfaceVarianceBumpMap("_SurfaceVarianceBumpMap", 2D) = "bump" {}
        _ParallaxMap("_ParallaxMap", 2D) = "white" {}
        _ParallaxMapMulti("_ParallaxMapMulti", 2D) = "white" {}
        _Parallax("_Parallax", Range(0, 1)) = 0.05
        _ParallaxRange("_ParallaxRange", Range(0, 2000)) = 200
        _ParallaxMinSamples("_ParallaxMinSamples", Range(1, 100)) = 2
        _ParallaxMaxSamples("_ParallaxMaxSamples", Range(1, 400)) = 50
        _PlanetOrigin("_PlanetOrigin", vector) = (0,0,0)
        _VesselOrigin("_VesselOrigin", vector) = (0,0,0)
        _PlanetRadius("_PlanetRadius", float) = 1
        _NoiseTex("_NoiseTex", 2D) = "white" {}
        _LowStart("_LowStart", float) = 0
        _LowEnd("_LowEnd", float) = 1
        _HighStart("_HighStart", float) = 2
        _HighEnd("_HighEnd", float) = 3
        _Metallic("_Metallic (Specular)", Range(0, 2)) = 0.308
        _MetallicTint("_MetallicTint", COLOR) = (0,0,0)
        _LightPos("_LightPos", vector) = (0, 0, 0)
        _LightCount("_LightCount", Range(0, 10)) = 0
        


        _Debug("Debug", COLOR) = (1,1,1)
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
                //#include "UnityStandardCore.cginc"
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
                    float3 eyeX : TEXCOORD6;    //eye x y and z
                    float3 eyeY : TEXCOORD14;
                    float3 eyeZ : TEXCOORD15;
                    float3 reverseEye : TEXCOORD9;
                    float3 sampleRatio : TEXCOORD7; //sample ratio x y and z
                    float reverseSampleRatio : TEXCOORD10;
                    float2 texcoord : TEXCOORD0;
                    float3 normal : TEXCOORD8;
                    float3 worldNormal : TEXCOORD12;
                    float3 vertexNormal : TEXCOORD13;
                    float2 uvX : TEXCOORD11;
                    float2 uvY : TEXCOORD18;
                    float2 uvZ : TEXCOORD16;
                    float3 blend : TEXCOORD17;
                };

                sampler2D _SurfaceTexture;
                sampler2D _SurfaceTextureMid;
                sampler2D _SurfaceTextureHigh;
                sampler2D _SteepTex;
                sampler2D _SurfaceVarianceTexture;
                float4 _SurfaceTexture_ST;
                float4 _SteepTex_ST;
                float _Strength;
                float _SteepPower;
                sampler2D _BumpMap;
                sampler2D _BumpMapMid;
                sampler2D _BumpMapHigh;
                sampler2D _BumpMapSteep;
                sampler2D _SurfaceVarianceBumpMap;
                float _Parallax;
                float _SurfaceVarianceTexturePow;
                float4 _SurfaceVarianceTexture_ST;
                float3 _PlanetOrigin;
                float3 _VesselOrigin;
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
                float3 _Debug;
                float _LowStart;
                float _LowEnd;
                float _HighStart;
                float _HighEnd;
                float _PlanetRadius;

                void parallax_vert2(float3 worldPos, float3 vertexNormal, float3x3 objectToTangent, out float3 eye, out float sampleRatio) 
                {
                    float3 worldSpaceViewDir = (worldPos.xyz - _WorldSpaceCameraPos.xyz);
                    eye = mul(objectToTangent, worldSpaceViewDir);
                    sampleRatio = 1 - dot(worldSpaceViewDir, vertexNormal);
                }
                float2 parallax_offset2(float fHeightMapScale, float3 eye, float sampleRatio, float2 texcoord, sampler2D heightMap, float4 lerpPercentage, int nMinSamples, int nMaxSamples)
                {

                    //floatPercentage is lowBlend, highBlend, midpoint, slope
                    float fParallaxLimit = -length(eye.xy) / eye.z;
                    fParallaxLimit *= fHeightMapScale;

                    float2 vOffsetDir = normalize(eye.xy);
                    float2 vMaxOffset = vOffsetDir * fParallaxLimit;

                    int nNumSamples = nMaxSamples;// (int)lerp(nMinSamples, nMaxSamples, saturate(sampleRatio));

                    float fStepSize = 1.0 / (float)nNumSamples;

                    float2 dx = ddx(texcoord);
                    float2 dy = ddy(texcoord);

                    float fCurrRayHeight = 1.0;
                    float2 vCurrOffset = float2(0, 0);
                    float2 vLastOffset = float2(0, 0);

                    float fLastSampledHeight = 1;
                    float fCurrSampledHeight = 1;   //Grey
                    float fCurrSampledHeight2 = 1;  //Red
                    float fCurrSampledHeight3 = 1;  //Green
                    float fCurrSampledHeight4 = 1;  //Blue
                    float fCurrSampledHeight5 = 1;  //Alpha

                    float heightR = 1;
                    fixed4 sampledColour = 1;

                    int nCurrSample = 0;

                    while (nCurrSample < nNumSamples)
                    {
                        sampledColour = tex2Dgrad(heightMap, texcoord + vCurrOffset, dx, dy);
                        fCurrSampledHeight2 = sampledColour.r;
                        fCurrSampledHeight3 = sampledColour.g;
                        fCurrSampledHeight4 = sampledColour.b;
                        fCurrSampledHeight5 = sampledColour.a;
                        if (lerpPercentage.z < 0.5)
                        {
                            fCurrSampledHeight = lerp(fCurrSampledHeight2, fCurrSampledHeight3, 1 - lerpPercentage.x);
                        }
                        else
                        {
                            fCurrSampledHeight = lerp(fCurrSampledHeight3, fCurrSampledHeight4, lerpPercentage.y);
                        }
                        fCurrSampledHeight = lerp(fCurrSampledHeight, fCurrSampledHeight5, 1 - lerpPercentage.w);
                        //fCurrSampledHeight = lerp(fCurrSampledHeight2, fCurrSampledHeight3, lerpPercentage);
                        if (fCurrSampledHeight > fCurrRayHeight)
                        {
                            float delta1 = fCurrSampledHeight - fCurrRayHeight;
                            float delta2 = (fCurrRayHeight + fStepSize) - fLastSampledHeight;

                            float ratio = delta1 / (delta1 + delta2);

                            vCurrOffset = (ratio)*vLastOffset + (1.0 - ratio) * vCurrOffset;

                            nCurrSample = nNumSamples + 1;
                        }
                        else
                        {
                            nCurrSample++;

                            fCurrRayHeight -= fStepSize;

                            vLastOffset = vCurrOffset;
                            vCurrOffset += fStepSize * vMaxOffset;

                            fLastSampledHeight = fCurrSampledHeight;
                        }
                    }

                    return vCurrOffset;
                }
                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;    //Use this when calculating slope
                    o.texcoord = v.texcoord;
                    half3 wNormal = UnityObjectToWorldNormal(v.normal);
                    half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    half tangentSign = v.tangent.w *unity_WorldTransformParams.w;
                    half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                    o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                    o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                    o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                    o.normal = mul(unity_ObjectToWorld, v.normal);
                    o.cameraDist = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));
                    o.worldNormal = mul(unity_ObjectToWorld, v.normal);
                    o.blend = pow(abs(o.worldNormal.xyz), _Strength);
                    o.blend /= o.blend.x + o.blend.y + o.blend.z;

                    //FROM https://medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a#048e
                    half3 tnormalX = half3(0, 0, 0);
                    half3 tnormalY = half3(0, 0, 0);
                    half3 tnormalZ = half3(0, 0, 0);
                    half3 axisSign = sign(o.worldNormal);
                    half3 tangentX = normalize(cross(o.worldNormal, half3(0, axisSign.x, 0)));
                    half3 bitangentX = normalize(cross(tangentX, o.worldNormal)) * axisSign.x;
                    half3x3 tbnX = half3x3(tangentX, bitangentX, o.worldNormal);
                    half3 tangentY = normalize(cross(o.worldNormal, half3(0, 0, axisSign.y)));
                    half3 bitangentY = normalize(cross(tangentY, o.worldNormal)) * axisSign.y;
                    half3x3 tbnY = half3x3(tangentY, bitangentY, o.worldNormal);       
                    half3 tangentZ = normalize(cross(o.worldNormal, half3(0, -axisSign.z, 0)));
                    half3 bitangentZ = normalize(-cross(tangentZ, o.worldNormal)) * axisSign.z;
                    half3x3 tbnZ = half3x3(tangentZ, bitangentZ, o.worldNormal);
                    half3 worldNormal = normalize(
                        clamp(mul(tnormalX, tbnX), -1, 1) * o.blend.x +
                        clamp(mul(tnormalY, tbnY), -1, 1) * o.blend.y +
                        clamp(mul(tnormalZ, tbnZ), -1, 1) * o.blend.z
                    );
                    float3 vertexNormal = normalize(o.worldNormal);

                    parallax_vert2(float3(o.worldPos), vertexNormal, tbnX, o.eyeX, o.sampleRatio.x);  //x
                    parallax_vert2(float3(o.worldPos), vertexNormal, tbnY, o.eyeY, o.sampleRatio.y);  //y
                    parallax_vert2(float3(o.worldPos), vertexNormal, tbnZ, o.eyeZ, o.sampleRatio.z);  //z

                    o.uvX = (o.worldPos.zy - _VesselOrigin.zy) * _SurfaceTexture_ST; // x facing plane
                    o.uvY = (o.worldPos.xz - _VesselOrigin.xz) * _SurfaceTexture_ST; // y facing plane
                    o.uvZ = (o.worldPos.xy - _VesselOrigin.xy) * _SurfaceTexture_ST; // z facing plane
                    
                    o.vertexNormal = vertexNormal;

                    return o;
                }
                float3x3 cotangent_frame(float3 normal, float3 position, float2 uv)  //Tangent matrix for tangent space
                {
                    float3 dp1 = ddx(position);
                    float3 dp2 = ddy(position) * _ProjectionParams.x;
                    float2 duv1 = ddx(uv);
                    float2 duv2 = ddy(uv) * _ProjectionParams.x;
                    float3 dp2perp = cross(dp2, normal);
                    float3 dp1perp = cross(normal, dp1);
                    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
                    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;
                    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
                    return float3x3(T * invmax, B * invmax, normal);
                }
                float heightBlendLow(float3 worldPos)
                {
                    float terrainHeight = distance(worldPos, _PlanetOrigin) - _PlanetRadius;

                    float blendLow = saturate((terrainHeight - _LowEnd) / (_LowStart - _LowEnd));
                    return blendLow;
                }
                float heightBlendHigh(float3 worldPos)
                {
                    float terrainHeight = distance(worldPos, _PlanetOrigin) - _PlanetRadius;

                    float blendHigh = saturate((terrainHeight - _HighStart) / (_HighEnd - _HighStart));
                    return blendHigh;
                }

                fixed4 SampleTriplanarTexture(sampler2D tex, float2 uvX, float2 uvY, float2 uvZ, float uvDistortion, float nextUVDist, float2 offsetSurfX, float2 offsetSurfY, float2 offsetSurfZ, float percentage, float3 triblend)
                {
                    fixed4 zoomLevel1X = tex2D(tex, uvX / uvDistortion + offsetSurfX);
                    fixed4 zoomLevel1Y = tex2D(tex, uvY / uvDistortion + offsetSurfY);
                    fixed4 zoomLevel1Z = tex2D(tex, uvZ / uvDistortion + offsetSurfZ);

                    fixed4 zoomLevel2X = tex2D(tex, uvX / nextUVDist);
                    fixed4 zoomLevel2Y = tex2D(tex, uvY / nextUVDist);
                    fixed4 zoomLevel2Z = tex2D(tex, uvZ / nextUVDist);

                    fixed4 actualX = lerp(zoomLevel1X, zoomLevel2X, percentage);
                    fixed4 actualY = lerp(zoomLevel1Y, zoomLevel2Y, percentage);
                    fixed4 actualZ = lerp(zoomLevel1Z, zoomLevel2Z, percentage);

                    fixed4 finalCol = actualX * triblend.x + actualY * triblend.y + actualZ * triblend.z;
                    return finalCol;
                }
                float3x3 UnpackTriplanarNormal(sampler2D _BumpMap, float2 uvX, float2 uvY, float2 uvZ, float uvDistortion, float nextUVDist, float2 offsetSurfX, float2 offsetSurfY, float2 offsetSurfZ, float percentage)
                {
                    float3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX / uvDistortion + offsetSurfX));
                    float3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY / uvDistortion + offsetSurfY));
                    float3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ / uvDistortion + offsetSurfZ));

                    float3 tnormalXZoom = UnpackNormal(tex2D(_BumpMap, uvX / nextUVDist));
                    float3 tnormalYZoom = UnpackNormal(tex2D(_BumpMap, uvY / nextUVDist));
                    float3 tnormalZZoom = UnpackNormal(tex2D(_BumpMap, uvZ / nextUVDist));

                    float3 tnormalCombinedX = lerp(tnormalX, tnormalXZoom, percentage);
                    float3 tnormalCombinedY = lerp(tnormalY, tnormalYZoom, percentage);
                    float3 tnormalCombinedZ = lerp(tnormalZ, tnormalZZoom, percentage);

                    return float3x3(tnormalCombinedX, tnormalCombinedY, tnormalCombinedZ);
                }
                fixed4 frag(v2f i) : SV_Target
                {
                    
                    //if (distance(i.worldPos, _PlanetOrigin) - _PlanetRadius > _LowEnd && distance(i.worldPos, _PlanetOrigin) - _PlanetRadius < _HighStart)
                    //{
                    //    return 1;
                    //}
                    
                    //return float4(blendLow, blendHigh, 0, 1);
                    //Slope calculation relative to the planet origin (mountain slopes). This value determines the blending of textures
                    float slope = abs(dot(normalize(i.worldPos - _PlanetOrigin), normalize(i.normal)));


                    ///////////////////////////////////////////////
                    //////////////Texture Zoom Levels//////////////
                    ///////////////////////////////////////////////
                    float2 surfaceTexcoord = (i.worldPos.xz * (_SurfaceTexture_ST.xy) + _SurfaceTexture_ST.zw);
                    float ZoomLevel = ((0.8 * pow(i.cameraDist, 0.18) - 0.5));
                    ZoomLevel = clamp(ZoomLevel, 1, 10);
                    int ClampedZoomLevel = floor(ZoomLevel);
                    if (ClampedZoomLevel >= 10)
                    {
                        ClampedZoomLevel = 10;
                        ZoomLevel = 10;
                    }
                    float percentage = (ZoomLevel - ClampedZoomLevel);
                    float uvDistortion = pow(ClampedZoomLevel, 3);
                    float nextUVDist = pow((ClampedZoomLevel + 1), 3);

                    half3 triblend = pow(abs(i.vertexNormal), _Strength * 1.4); //Triblend determines blending of triplanar textures
                    triblend /= max(dot(triblend, half3(1, 1, 1)), 0);
                    fixed4 multiTexBlendX = tex2D(_NoiseTex, i.uvX * _SurfaceVarianceTextureScale);
                    fixed4 multiTexBlendY = tex2D(_NoiseTex, i.uvY * _SurfaceVarianceTextureScale);  //Triplanar map the noise-tex
                    fixed4 multiTexBlendZ = tex2D(_NoiseTex, i.uvZ * _SurfaceVarianceTextureScale);
                    fixed4 multiTexBlend = multiTexBlendX * triblend.x + multiTexBlendY * triblend.y + multiTexBlendZ * triblend.z;
                    float multiTexBlendPow = pow(multiTexBlend, _SurfaceVarianceTexturePow);

                    /////////////////////////////////
                    ///////PARALLAX CALCULATION//////
                    /////////////////////////////////

                    float blendLow = heightBlendLow(i.worldPos);
                    float blendHigh = heightBlendHigh(i.worldPos);
                    float midPoint = (distance(i.worldPos, _PlanetOrigin) - _PlanetRadius) / (_HighStart + _LowEnd);    //EXACTLY halfway between lowEnd and highStart
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
                    slope = pow(slope, _SteepPower);
                    if (i.cameraDist < _ParallaxRange)     //Shader etiquette 101 is to use minimal conditional statements but this massively improves performance
                    {
                        _ParallaxMaxSamples = _ParallaxMaxSamples * pow(parallaxCameraRangeIntensity, 5) + 5;

                        offsetSurfX = parallax_offset2(_Parallax, i.eyeX, i.sampleRatio.x, i.uvX, _ParallaxMap, float4(blendLow, blendHigh, midPoint, slope), _ParallaxMinSamples, _ParallaxMaxSamples);  //POSITIVE X
                        offsetSurfZ = parallax_offset2(_Parallax, i.eyeZ, i.sampleRatio.z, i.uvZ, _ParallaxMap, float4(blendLow, blendHigh, midPoint, slope), _ParallaxMinSamples, _ParallaxMaxSamples);  //POSITIVE Z
                        offsetSurfY = parallax_offset2(_Parallax, i.eyeY, i.sampleRatio.y, i.uvY, _ParallaxMap, float4(blendLow, blendHigh, midPoint, slope), _ParallaxMinSamples, _ParallaxMaxSamples);  //POSITIVE Y
                    }



                    


                    //Noisemap tiling
                    

                    //fixed4 colX = tex2D(_SurfaceTexture, i.uvX / uvDistortion + offsetSurfX);
                    //fixed4 colY = tex2D(_SurfaceTexture, i.uvY / uvDistortion + offsetSurfY);   //surface tex with parallax offset
                    //fixed4 colZ = tex2D(_SurfaceTexture, i.uvZ / uvDistortion + offsetSurfZ);   //UvDistortion refers to the texture zoom-out levels and does not affect parallax effect
                    //
                    //fixed4 colX2 = tex2D(_SurfaceTexture, i.uvX / nextUVDist);
                    //fixed4 colY2 = tex2D(_SurfaceTexture, i.uvY / nextUVDist);    //surface tex zoomed out
                    //fixed4 colZ2 = tex2D(_SurfaceTexture, i.uvZ / nextUVDist);
                    //
                    //fixed4 colXSteep = tex2D(_SteepTex, i.uvX / uvDistortion);
                    //fixed4 colYSteep = tex2D(_SteepTex, i.uvY / uvDistortion);    //steep tex
                    //fixed4 colZSteep = tex2D(_SteepTex, i.uvZ / uvDistortion);
                    //
                    //fixed4 colXSteep2 = tex2D(_SteepTex, i.uvX / nextUVDist);
                    //fixed4 colYSteep2 = tex2D(_SteepTex, i.uvY / nextUVDist);    //steep tex
                    //fixed4 colZSteep2 = tex2D(_SteepTex, i.uvZ / nextUVDist);
                    //
                    //fixed4 colXMulti = tex2D(_SurfaceVarianceTexture, (i.uvX * _SurfaceVarianceTexture_ST) / uvDistortion + offsetSurfX);   
                    //fixed4 colYMulti = tex2D(_SurfaceVarianceTexture, (i.uvY * _SurfaceVarianceTexture_ST) / uvDistortion + offsetSurfY);   //surface variance tex
                    //fixed4 colZMulti = tex2D(_SurfaceVarianceTexture, (i.uvZ * _SurfaceVarianceTexture_ST) / uvDistortion + offsetSurfZ);
                    //
                    //fixed4 actualColXSteep = lerp(colXSteep, colXSteep2, percentage);
                    //fixed4 actualColYSteep = lerp(colYSteep, colYSteep2, percentage);   //Blend slope texture with the zoomed out version
                    //fixed4 actualColZSteep = lerp(colZSteep, colZSteep2, percentage);
                    //
                    //fixed4 actualColX = lerp(colX, colX2, percentage);
                    //fixed4 actualColY = lerp(colY, colY2, percentage);  //UV distortion blending
                    //fixed4 actualColZ = lerp(colZ, colZ2, percentage);
                    //
                    //fixed4 blendColX = lerp(actualColX, colXMulti, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                    //fixed4 blendColY = lerp(actualColY, colYMulti, pow(multiTexBlend, _SurfaceVarianceTexturePow)); //multi-tex blending
                    //fixed4 blendColZ = lerp(actualColZ, colZMulti, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                    //
                    //fixed4 finalColX = lerp(blendColX, actualColXSteep, 1 - slope);
                    //fixed4 finalColY = lerp(blendColY, actualColYSteep, 1 - slope); //Final albedo color before lighting is applied
                    //fixed4 finalColZ = lerp(blendColZ, actualColZSteep, 1 - slope);
                    //
                    //
                    //finalColX.a = lerp(actualColX.a, colXMulti.a, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                    //finalColY.a = lerp(actualColY.a, colYMulti.a, pow(multiTexBlend, _SurfaceVarianceTexturePow));  //Lerp the alpha channel for specular lighting
                    //finalColZ.a = lerp(actualColZ.a, colZMulti.a, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                    //
                    //fixed4 col = finalColX * triblend.x + finalColY * triblend.y + finalColZ * triblend.z;
                    

                    

                    //return float4(blendLow, 0, blendHigh, 1);
                    //return midPoint;

                    fixed4 surfaceTexLow = SampleTriplanarTexture(_SurfaceTexture, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage, triblend);
                    fixed4 surfaceTexMid = SampleTriplanarTexture(_SurfaceTextureMid, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage, triblend);
                    fixed4 surfaceTexHigh = SampleTriplanarTexture(_SurfaceTextureHigh, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage, triblend);
                    fixed4 steepTex = SampleTriplanarTexture(_SteepTex, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage, triblend);

                    fixed4 surfaceCol = 0;
                    
                    if (midPoint < 0.5)
                    {
                        //Tex blend uses blendLow
                         surfaceCol = lerp(surfaceTexLow, surfaceTexMid, 1 - blendLow);
                    }
                    else
                    {
                        //Tex blend uses blendHigh
                         surfaceCol = lerp(surfaceTexMid, surfaceTexHigh, blendHigh);
                    }
                    surfaceCol = lerp(surfaceCol, steepTex, 1 - slope);
                    fixed4 col = surfaceCol;

                    // Tangent space normal maps

                    float3x3 lowNormal = UnpackTriplanarNormal(_BumpMap, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage);
                    float3x3 midNormal = UnpackTriplanarNormal(_BumpMapMid, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage);
                    float3x3 highNormal = UnpackTriplanarNormal(_BumpMapHigh, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage);
                    float3x3 steepNormal = UnpackTriplanarNormal(_BumpMapSteep, i.uvX, i.uvY, i.uvZ, uvDistortion, nextUVDist, offsetSurfX, offsetSurfY, offsetSurfZ, percentage);
                    float3 tnormalX = lowNormal[0];
                    float3 tnormalY = lowNormal[1];
                    float3 tnormalZ = lowNormal[2];
                    float3 tnormalMidX = midNormal[0];
                    float3 tnormalMidY = midNormal[1];
                    float3 tnormalMidZ = midNormal[2];
                    float3 tnormalHighX = highNormal[0];
                    float3 tnormalHighY = highNormal[1];
                    float3 tnormalHighZ = highNormal[2];
                    float3 tnormalSteepX = steepNormal[0];
                    float3 tnormalSteepY = steepNormal[1];
                    float3 tnormalSteepZ = steepNormal[2];
                    
                    if (midPoint < 0.5)
                    {
                        //Tex blend uses blendLow
                        tnormalX = lerp(tnormalX, tnormalMidX, 1 - blendLow);
                        tnormalY = lerp(tnormalY, tnormalMidY, 1 - blendLow);
                        tnormalZ = lerp(tnormalZ, tnormalMidZ, 1 - blendLow);
                    }
                    else
                    {
                        //Tex blend uses blendHigh
                        tnormalX = lerp(tnormalMidX, tnormalHighX, blendHigh);
                        tnormalY = lerp(tnormalMidY, tnormalHighY, blendHigh);
                        tnormalZ = lerp(tnormalMidZ, tnormalHighZ, blendHigh);
                    }

                    tnormalX = lerp(tnormalX, tnormalSteepX, 1 - slope);
                    tnormalY = lerp(tnormalY, tnormalSteepY, 1 - slope);
                    tnormalZ = lerp(tnormalZ, tnormalSteepZ, 1 - slope);


                    //float3 tnormalX = UnpackNormal(tex2D(_BumpMap, i.uvX / uvDistortion + offsetSurfX));
                    //float3 tnormalY = UnpackNormal(tex2D(_BumpMap, i.uvY / uvDistortion + offsetSurfY));
                    //float3 tnormalZ = UnpackNormal(tex2D(_BumpMap, i.uvZ / uvDistortion + offsetSurfZ));
                    //half3 tnormalXZoom = UnpackNormal(tex2D(_BumpMap, i.uvX / nextUVDist));
                    //half3 tnormalYZoom = UnpackNormal(tex2D(_BumpMap, i.uvY / nextUVDist));
                    //half3 tnormalZZoom = UnpackNormal(tex2D(_BumpMap, i.uvZ / nextUVDist));
                    //
                    //half3 tnormalXVariance = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, i.uvX / uvDistortion + offsetSurfX));
                    //half3 tnormalYVariance = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, i.uvY / uvDistortion + offsetSurfY));
                    //half3 tnormalZVariance = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, i.uvZ / uvDistortion + offsetSurfZ));
                    //
                    //half3 tnormalXVarianceZoom = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, i.uvX / nextUVDist));
                    //half3 tnormalYVarianceZoom = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, i.uvY / nextUVDist));
                    //half3 tnormalZVarianceZoom = UnpackNormal(tex2D(_SurfaceVarianceBumpMap, i.uvZ / nextUVDist));
                    //
                    //half3 normalXZoomCombined = lerp(tnormalX, tnormalXZoom, percentage);
                    //half3 normalYZoomCombined = lerp(tnormalY, tnormalYZoom, percentage);
                    //half3 normalZZoomCombined = lerp(tnormalZ, tnormalZZoom, percentage);
                    //
                    //half3 normalXVarianceZoomCombined = lerp(tnormalXVariance, tnormalXVarianceZoom, percentage);
                    //half3 normalYVarianceZoomCombined = lerp(tnormalYVariance, tnormalYVarianceZoom, percentage);
                    //half3 normalZVarianceZoomCombined = lerp(tnormalZVariance, tnormalZVarianceZoom, percentage);
                    //
                    //tnormalX = lerp(normalXZoomCombined, normalXVarianceZoomCombined, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                    //tnormalY = lerp(normalYZoomCombined, normalYVarianceZoomCombined, pow(multiTexBlend, _SurfaceVarianceTexturePow));
                    //tnormalZ = lerp(normalZZoomCombined, normalZVarianceZoomCombined, pow(multiTexBlend, _SurfaceVarianceTexturePow));

                    // Get the sign (-1 or 1) of the surface normal
                    float3 axisSign = sign(i.worldNormal);
                    // Construct tangent to world matrices for each axis
                    float3 tangentX = normalize(cross(i.worldNormal, half3(0, axisSign.x, 0)));
                    float3 bitangentX = normalize(cross(tangentX, i.worldNormal)) * axisSign.x;
                    half3x3 tbnX = half3x3(tangentX, bitangentX, i.worldNormal);
                    float3 tangentY = normalize(cross(i.worldNormal, half3(0, 0, axisSign.y)));
                    float3 bitangentY = normalize(cross(tangentY, i.worldNormal)) * axisSign.y;
                    half3x3 tbnY = half3x3(tangentY, bitangentY, i.worldNormal);
                    float3 tangentZ = normalize(cross(i.worldNormal, half3(0, -axisSign.z, 0)));
                    float3 bitangentZ = normalize(-cross(tangentZ, i.worldNormal)) * axisSign.z;
                    half3x3 tbnZ = half3x3(tangentZ, bitangentZ, i.worldNormal);
                    // Apply tangent to world matrix and triblend
                    // Using clamp() because the cross products may be NANs
                    half3 worldNormal = normalize(
                        clamp(mul(tnormalX, tbnX), -1, 1) * i.blend.x +
                        clamp(mul(tnormalY, tbnY), -1, 1) * i.blend.y +
                        clamp(mul(tnormalZ, tbnZ), -1, 1) * i.blend.z
                    );
                    float3 vertexNormal = normalize(i.worldNormal);

                    //////////////////
                    /////LIGHTING/////
                    //////////////////
                    float rangePercentage = 1;// = 1 - clamp(distance(_LightPos.xyz, i.worldPos) / 2000, 0, 1);
                    half ndotl = saturate(dot(worldNormal, normalize(_LightPos.xyz - i.worldPos)));
                    half3 ambient = ShadeSH9(half4(worldNormal, 1));
                    half3 lighting = clamp(float3(1,1,1) * ndotl * rangePercentage + ambient, 0, 1);
                    float3 lightDir = normalize(_LightPos.xyz - i.worldPos);
                    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                    float3 halfVector = normalize(lightDir + viewDir);
                    float3 specular = saturate(_MetallicTint * pow(dot(halfVector, i.normal), _Metallic * 100));

                    return fixed4(col.rgb * lighting + (specular.rgb * col.a), 1);
                }
                ENDCG
            }
        }
        Fallback "Diffuse"
}