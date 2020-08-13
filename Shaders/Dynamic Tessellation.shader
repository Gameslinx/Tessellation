// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/ParallaxOcclusion2"
{
	Properties
	{
		_SurfaceTexture("_SurfaceTexture", 2D) = "white" {}
		[NoScaleOffset] _SurfaceTextureMid("_SurfaceTextureMid", 2D) = "white" {}
		[NoScaleOffset] _SurfaceTextureHigh("_SurfaceTextureHigh", 2D) = "white" {}
		[NoScaleOffset] _SteepTex("_SteepTex", 2D) = "white" {}
		[NoScaleOffset] _InfluenceMap("_InfluenceMap", 2D) = "white" {}

		[NoScaleOffset] _BumpMap("_BumpMap", 2D) = "bump" {}
		[NoScaleOffset] _BumpMapMid("_BumpMapMid", 2D) = "bump" {}
		[NoScaleOffset] _BumpMapHigh("_BumpMapHigh", 2D) = "bump" {}
		[NoScaleOffset] _BumpMapSteep("_BumpMapSteep", 2D) = "bump" {}

		[NoScaleOffset] _DispTex("Displacement Texture", 2D) = "white" {}

		_LowStart("_LowStart", float) = 0
		_LowEnd("_LowEnd", float) = 1
		_HighStart("_HighStart", float) = 2
		_HighEnd("_HighEnd", float) = 3

		_displacement_scale("Displacement Scale", Range(0, 2)) = 1
		_SteepPower("_SteepPower", Range(0.01, 50)) = 1
		_Strength("_Strength", Range(0.001, 100)) = 4
		_LightPos("_LightPos", vector) = (0,0,0)

		_Metallic("_Metallic", Range(0.001, 3)) = 0.2
		_MetallicTint("_MetallicTint", COLOR) = (0,0,0)


		_OceanColor("_OceanColor", COLOR) = (0,0,0,1)
		_OceanHeight("_OceanHeight", Range(-5, 5)) = 0
		_OceanSpecular("_OceanSpecular", Range(0.001, 10)) = 0
		_OceanSpecColor("_OceanSpecColor", COLOR) = (0,0,0)

		_PlanetOrigin("_PlanetOrigin", vector) = (0,0,0)
		_PlanetRadius("_PlanetRadius", float) = 100000
		_LowStart("_LowStart", float) = 0
		_LowEnd("_LowEnd", float) = 1
		_HighStart("_HighStart", float) = 2
		_HighEnd("_HighEnd", float) = 3
		_TessellationEdgeLength("_TessellationEdgeLength", Range(0.0001, 10)) = 0.01
	}
	SubShader
	{
		
		Pass
		{
			Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }
			CGPROGRAM
			// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
			#pragma exclude_renderers gles
			#pragma vertex vertex_shader			
			#pragma hull hull_shader
			#pragma domain domain_shader
			#pragma fragment pixel_shader
			#include "UnityCG.cginc"
			#include "UnityStandardBRDF.cginc"
			#include "AutoLight.cginc"

			sampler2D _SurfaceTexture;
			sampler2D _SurfaceTextureMid;
			sampler2D _SurfaceTextureHigh;
			sampler2D _SteepTex;
			sampler2D _displacement;
			float2 _SurfaceTexture_ST;
			sampler2D _DispTex;
			float2 _DispTex_ST;
			float2 _displacement_ST;
			float _tessellation_scale;
			float _displacement_scale;
			float _offsetX,_offsetY,_offsetZ;
			float3 _LightPos;
			float _Metallic;
			float3 _MetallicTint;
			float _Strength;
			sampler2D _BumpMap;
			sampler2D _BumpMapMid;
			sampler2D _BumpMapHigh;
			sampler2D _BumpMapSteep;
			float _TessellationEdgeLength;
			float3 _PlanetOrigin;
			float _PlanetRadius;
			float _LowStart;
			float _LowEnd;
			float _HighStart;
			float _HighEnd;
			sampler2D _InfluenceMap;
			float _SteepPower;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float3 color : COLOR;
			};

			struct VertexOutput
			{
				float4 screen_vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 world_vertex : TEXCOORD1;
				float3 normalDir : TEXCOORD2;
				float3 normal : NORMAL;
				float3 worldNormal : TEXCOORD5;
				float3 blend : TEXCOORD6;
				float3x2 UVs : TEXCOORD7;
				float3 color : COLOR;
				LIGHTING_COORDS(3, 4)	//this doesn't work yet
			};
			fixed4 lerpSurfaceColor(fixed4 low, fixed4 mid, fixed4 high, fixed4 steep, float midPoint, float slope, float blendLow, float blendHigh)
			{
				fixed4 col;
				if (midPoint < 0.5)
				{
					col = lerp(low, mid, 1 - blendLow);
				}
				else
				{
					col = lerp(mid, high, blendHigh);
				}
				col = lerp(col, steep, 1 - slope);
				return col;
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
			fixed4 SampleDisplacementTriplanarTexture(sampler2D tex, float3x2 UVs, float uvDistortion, float nextUVDist, float percentage, float3 triblend, float slope, float3 world_vertex)
			{
				float blendLow = heightBlendLow(world_vertex);
				float blendHigh = heightBlendHigh(world_vertex);
				float midPoint = (distance(world_vertex, _PlanetOrigin) - _PlanetRadius) / (_HighStart + _LowEnd);

				fixed4 zoomLevel1X = tex2Dlod(tex, float4(((UVs[0]) / uvDistortion), 1, 1));
				fixed4 zoomLevel1Y = tex2Dlod(tex, float4(((UVs[1]) / uvDistortion), 1, 1));
				fixed4 zoomLevel1Z = tex2Dlod(tex, float4(((UVs[2]) / uvDistortion), 1, 1));

				fixed4 zoomLevel2X = tex2Dlod(tex, float4(((UVs[0]) / nextUVDist), 1, 1));
				fixed4 zoomLevel2Y = tex2Dlod(tex, float4(((UVs[1]) / nextUVDist), 1, 1));
				fixed4 zoomLevel2Z = tex2Dlod(tex, float4(((UVs[2]) / nextUVDist), 1, 1));

				fixed4 actualX = lerp(zoomLevel1X, zoomLevel2X, percentage);
				fixed4 actualY = lerp(zoomLevel1Y, zoomLevel2Y, percentage);
				fixed4 actualZ = lerp(zoomLevel1Z, zoomLevel2Z, percentage);

				fixed4 finalCol = actualX * triblend.x + actualY * triblend.y + actualZ * triblend.z;

				fixed4 finalColLow = finalCol.r;
				fixed4 finalColMid = finalCol.g;
				fixed4 finalColHigh = finalCol.b;
				fixed4 finalColSteep = finalCol.a;

				fixed4 displacement = lerpSurfaceColor(finalColLow, finalColMid, finalColHigh, finalColSteep, midPoint, slope, blendLow, blendHigh);

				return displacement;
			}
			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;
				o.uv = v.uv;
				o.world_vertex = mul(unity_ObjectToWorld, v.vertex);
				o.normalDir = normalize(unity_WorldToObject[0].xyz * v.normal.x + unity_WorldToObject[1].xyz * v.normal.y + unity_WorldToObject[2].xyz * v.normal.z);
				o.worldNormal = mul(unity_ObjectToWorld, v.normal);
				o.blend = pow(abs(o.worldNormal.xyz), _Strength);
				o.blend /= o.blend.x + o.blend.y + o.blend.z;
				o.color = v.color;

				float2 uvX = o.world_vertex.yz * _SurfaceTexture_ST;
				float2 uvY = o.world_vertex.xz * _SurfaceTexture_ST;
				float2 uvZ = o.world_vertex.xy * _SurfaceTexture_ST;
				o.UVs = float3x2(uvX, uvY, uvZ);
				o.normal = v.normal;
				float slope = abs(dot(normalize(o.world_vertex - _PlanetOrigin), normalize(o.normal)));



				float cameraDist = distance(_WorldSpaceCameraPos, o.world_vertex);
				float ZoomLevel = ((1.5 * pow(cameraDist - 100, 0.18)));
				ZoomLevel = clamp(ZoomLevel, 1, 10);
				int ClampedZoomLevel = floor(ZoomLevel);
				if (ClampedZoomLevel >= 10)
				{
					ClampedZoomLevel = 10;
					ZoomLevel = 10;
				}
				float percentage = (ZoomLevel - ClampedZoomLevel);
				float uvDistortion = pow(2, ClampedZoomLevel - 1);
				float nextUVDist = pow(2, ClampedZoomLevel);
				float3 displacement = SampleDisplacementTriplanarTexture(_DispTex, o.UVs, uvDistortion, nextUVDist, percentage, o.blend, slope, o.world_vertex) * lerp(uvDistortion, nextUVDist, percentage);
				displacement = (-0 * lerp(uvDistortion, nextUVDist, percentage)) + displacement;
				v.vertex.xyz += (displacement * _displacement_scale * v.normal);
				
				
				o.screen_vertex = UnityObjectToClipPos(v.vertex);

				


				return o;
			}

			struct tessellation
			{
				float4 vertex : INTERNALTESSPOS;
				float3 normal : NORMAL;
				float2 texcoord0 : TEXCOORD0;
				float3 color : COLOR;
			};

			struct OutputPatchConstant
			{
				float edge[3]: SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			tessellation vertex_shader(VertexInput v)
			{
				tessellation o;
				o.vertex = v.vertex;
				o.normal = v.normal;
				o.texcoord0 = v.uv;
				o.color = v.color;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
			float TessellationEdgeFactor(tessellation cp0, tessellation cp1)
			{
				float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
				float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
				float edgeLength = distance(p0, p1);

				float3 edgeCenter = (p0 + p1) * 0.5;
				float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);


				return edgeLength * pow(_ScreenParams.y / (_TessellationEdgeLength * viewDistance), 2);

			}
			OutputPatchConstant constantsHS(InputPatch<tessellation,3> patch)
			{
				OutputPatchConstant o;
				float t = _tessellation_scale;
				//o.edge[0] = o.edge[1] = o.edge[2] = o.inside = t;

				o.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
				o.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
				o.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
				o.inside = (o.edge[0] + o.edge[1] + o.edge[2]) * (1 / 3.0);

				

				return o;
			}

			[domain("tri")]
			[partitioning("fractional_even")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("constantsHS")]
			[outputcontrolpoints(3)]
			tessellation hull_shader(InputPatch<tessellation,3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}
			
			[domain("tri")]
			VertexOutput domain_shader(OutputPatchConstant tessFactors,const OutputPatch<tessellation,3> vs, float3 d:SV_DomainLocation)
			{
				VertexInput v;
				v.vertex = vs[0].vertex * d.x + vs[1].vertex * d.y + vs[2].vertex * d.z;
				v.normal = vs[0].normal * d.x + vs[1].normal * d.y + vs[2].normal * d.z;
				v.uv = vs[0].texcoord0 * d.x + vs[1].texcoord0 * d.y + vs[2].texcoord0 * d.z;
				v.color = vs[0].color * d.x + vs[1].color * d.y + vs[2].color * d.z;
				v.vertex.xyz += ((tex2Dlod(_SurfaceTexture,float4(v.uv * _SurfaceTexture_ST,0.0,0.0)).xyz * normalize(v.normal)) * _displacement_scale);	//mcfucking YEET that shit
				VertexOutput o = vert(v);
				return o;
			}
			float2x3 lighting(float3 worldNormal, float3 worldPos, float3 normal)
			{
				float ndotl = saturate(dot(worldNormal, normalize(_LightPos.xyz - worldPos)));
				float3 ambient = ShadeSH9(float4(worldNormal, 1));
				float3 lighting = clamp(float3(1, 1, 1) * ndotl + ambient, 0, 1);
				float3 lightDir = normalize(_LightPos.xyz - worldPos);
				float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				float3 floatVector = normalize(lightDir + viewDir);
				float3 specular = saturate(_MetallicTint * pow(dot(floatVector, normal), _Metallic * 100));
				return float2x3(lighting, specular);
			}
			float3 tangentSpace(float3 worldNormal, float3 blend, float3x3 normals)	//proper normal mapping like a chad
			{
				float3 tnormalX = normals[0];
				float3 tnormalY = normals[1];
				float3 tnormalZ = normals[2];
				float3 axisSign = sign(worldNormal);
				float3 tangentX = normalize(cross(worldNormal, float3(0, axisSign.x, 0)));
				float3 bitangentX = normalize(cross(tangentX, worldNormal)) * axisSign.x;
				float3x3 tbnX = float3x3(tangentX, bitangentX, worldNormal);
				float3 tangentY = normalize(cross(worldNormal, float3(0, 0, axisSign.y)));
				float3 bitangentY = normalize(cross(tangentY, worldNormal)) * axisSign.y;
				float3x3 tbnY = float3x3(tangentY, bitangentY, worldNormal);
				float3 tangentZ = normalize(cross(worldNormal, float3(0, -axisSign.z, 0)));
				float3 bitangentZ = normalize(-cross(tangentZ, worldNormal)) * axisSign.z;
				float3x3 tbnZ = float3x3(tangentZ, bitangentZ, worldNormal);
				worldNormal = normalize(
					clamp(mul(tnormalX, tbnX), -1, 1) * blend.x +
					clamp(mul(tnormalY, tbnY), -1, 1) * blend.y +
					clamp(mul(tnormalZ, tbnZ), -1, 1) * blend.z
				);
				float3 vertexNormal = normalize(worldNormal);
				return vertexNormal;
			}
			fixed4 SampleTriplanarTexture(sampler2D tex, float3x2 UVs, float uvDistortion, float nextUVDist, float percentage, float3 triblend)
			{
				fixed4 zoomLevel1X = tex2D(tex, ((UVs[0]) / uvDistortion));
				fixed4 zoomLevel1Y = tex2D(tex, ((UVs[1]) / uvDistortion));
				fixed4 zoomLevel1Z = tex2D(tex, ((UVs[2]) / uvDistortion));

				fixed4 zoomLevel2X = tex2D(tex, ((UVs[0]) / nextUVDist));
				fixed4 zoomLevel2Y = tex2D(tex, ((UVs[1]) / nextUVDist));
				fixed4 zoomLevel2Z = tex2D(tex, ((UVs[2]) / nextUVDist));

				fixed4 actualX = lerp(zoomLevel1X, zoomLevel2X, percentage);
				fixed4 actualY = lerp(zoomLevel1Y, zoomLevel2Y, percentage);
				fixed4 actualZ = lerp(zoomLevel1Z, zoomLevel2Z, percentage);

				fixed4 finalCol = actualX * triblend.x + actualY * triblend.y + actualZ * triblend.z;
				return finalCol;
			}
			
			float3x3 UnpackTriplanarNormal(sampler2D _BumpMap, float3x2 UVs, float uvDistortion, float nextUVDist, float percentage)
			{
				float3 tnormalX = UnpackNormal(tex2D(_BumpMap, ((UVs[0]) / uvDistortion)));
				float3 tnormalY = UnpackNormal(tex2D(_BumpMap, ((UVs[1]) / uvDistortion)));
				float3 tnormalZ = UnpackNormal(tex2D(_BumpMap, ((UVs[2]) / uvDistortion)));

				float3 tnormalXZoom = UnpackNormal(tex2D(_BumpMap, ((UVs[0]) / nextUVDist)));
				float3 tnormalYZoom = UnpackNormal(tex2D(_BumpMap, ((UVs[1]) / nextUVDist)));
				float3 tnormalZZoom = UnpackNormal(tex2D(_BumpMap, ((UVs[2]) / nextUVDist)));

				float3 tnormalCombinedX = lerp(tnormalX, tnormalXZoom, percentage);
				float3 tnormalCombinedY = lerp(tnormalY, tnormalYZoom, percentage);
				float3 tnormalCombinedZ = lerp(tnormalZ, tnormalZZoom, percentage);

				return float3x3(tnormalCombinedX, tnormalCombinedY, tnormalCombinedZ);
			}
			
			
			float3x3 lerpSurfaceNormal(float3x3 low, float3x3 mid, float3x3 high, float3x3 steep, float midPoint, float slope, float blendLow, float blendHigh)
			{
				float3x3 col;
				if (midPoint < 0.5)
				{
					col = lerp(low, mid, 1 - blendLow);
				}
				else
				{
					col = lerp(mid, high, blendHigh);
				}
				col = lerp(col, steep, 1 - slope);
				return col;
			}
			float4 pixel_shader(VertexOutput ps) : SV_TARGET
			{
				float cameraDist = distance(_WorldSpaceCameraPos, ps.world_vertex);
				float ZoomLevel = ((1.5 * pow(cameraDist - 100, 0.18)));
				ZoomLevel = clamp(ZoomLevel, 1, 10);
				int ClampedZoomLevel = floor(ZoomLevel);
				if (ClampedZoomLevel >= 10)
				{
					ClampedZoomLevel = 10;
					ZoomLevel = 10;
				}
				float percentage = (ZoomLevel - ClampedZoomLevel);
				float uvDistortion = pow(2, ClampedZoomLevel - 1);
				float nextUVDist = pow(2, ClampedZoomLevel);


				float slope = abs(dot(normalize(ps.world_vertex - _PlanetOrigin), normalize(ps.normal)));
				slope = pow(slope, _SteepPower);

				

				fixed4 lowTex = SampleTriplanarTexture(_SurfaceTexture, ps.UVs,uvDistortion, nextUVDist, percentage, ps.blend);
				fixed4 midTex = SampleTriplanarTexture(_SurfaceTextureMid, ps.UVs,uvDistortion, nextUVDist, percentage, ps.blend);
				fixed4 highTex = SampleTriplanarTexture(_SurfaceTextureHigh, ps.UVs,uvDistortion, nextUVDist, percentage, ps.blend);
				fixed4 steepTex = SampleTriplanarTexture(_SteepTex, ps.UVs,uvDistortion, nextUVDist, percentage, ps.blend);
				

				float3x3 lowNormal = UnpackTriplanarNormal(_BumpMap, ps.UVs, uvDistortion, nextUVDist, percentage);
				float3x3 midNormal = UnpackTriplanarNormal(_BumpMapMid, ps.UVs, uvDistortion, nextUVDist, percentage);
				float3x3 highNormal = UnpackTriplanarNormal(_BumpMapHigh, ps.UVs, uvDistortion, nextUVDist, percentage);
				float3x3 steepNormal = UnpackTriplanarNormal(_BumpMapSteep, ps.UVs, uvDistortion, nextUVDist, percentage);

				float blendLow = heightBlendLow(ps.world_vertex);
				float blendHigh = heightBlendHigh(ps.world_vertex);
				float midPoint = (distance(ps.world_vertex, _PlanetOrigin) - _PlanetRadius) / (_HighStart + _LowEnd);

				float luminosityLow = (lowTex.r * 0.21 + lowTex.g * 0.72 + lowTex.b * 0.07);
				float luminosityMid = (midTex.r * 0.21 + midTex.g * 0.72 + midTex.b * 0.07);
				float luminosityHigh = (highTex.r * 0.21 + highTex.g * 0.72 + highTex.b * 0.07);

				fixed4 influenceTex = SampleTriplanarTexture(_InfluenceMap, ps.UVs, uvDistortion, nextUVDist, percentage, ps.blend);
				lowTex.rgb = lerp(ps.color * (luminosityLow), lowTex.rgb, influenceTex.r);
				midTex.rgb = lerp(ps.color * (luminosityMid), midTex.rgb, influenceTex.g);
				highTex.rgb = lerp(ps.color * (luminosityHigh), highTex.rgb, influenceTex.b);

				fixed4 surfaceCol = lerpSurfaceColor(lowTex, midTex, highTex, steepTex, midPoint, slope, blendLow, blendHigh);
				float3x3 surfaceNormal = lerpSurfaceNormal(lowNormal, midNormal, highNormal, steepNormal, midPoint, slope, blendLow, blendHigh);


				float3 ambient = ShadeSH9(float4(ps.normalDir, 1));
				float atten = LIGHT_ATTENUATION(ps) + ambient;
				float3 normalLighting = tangentSpace(ps.worldNormal, ps.blend, surfaceNormal);
				float2x3 lightingData = lighting(normalLighting, ps.world_vertex, ps.normal);
				float3 lightColor = lightingData[0];
				float3 specular = lightingData[1];
				surfaceCol = surfaceCol * (float4(lightColor, 1)) + float4(specular, 1) * (surfaceCol.a);
				return surfaceCol;
			}
			ENDCG
		}
		Pass	//water fuckery
		{ 
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma exclude_renderers gles


			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			float4 _OceanColor;
			float3 _LightPos;
			float3 _OceanSpecColor;
			float _OceanSpecular;
			float _OceanHeight;
			float _PlanetOrigin;
			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			struct v2f
			{
				float4 screen_vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 normal : NORMAL;
				float3 worldNormal : TEXCOORD1;
			};
			v2f vert(VertexInput v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);;
				o.normal = v.normal;
				float slope = 1 - abs(dot(normalize(o.worldPos - _PlanetOrigin), normalize(o.normal)));
				v.vertex.xyz += 1 * v.normal * _OceanHeight * pow(slope, 10);
				o.screen_vertex = UnityObjectToClipPos(v.vertex);
				o.worldNormal = mul(unity_ObjectToWorld, v.normal);
				return o;
			}
			float2x3 lighting(float3 worldNormal, float3 worldPos, float3 normal)
			{
				float ndotl = saturate(dot(worldNormal, normalize(_LightPos.xyz - worldPos)));
				float3 ambient = ShadeSH9(float4(worldNormal, 1));
				float3 lighting = clamp(float3(1, 1, 1) * ndotl + ambient, 0, 1);
				float3 lightDir = normalize(_LightPos.xyz - worldPos);
				float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				float3 floatVector = normalize(lightDir + viewDir);
				float3 specular = saturate(_OceanSpecColor * pow(dot(floatVector, normal), _OceanSpecular * 100));
				return float2x3(lighting, specular);
			}
			fixed4 frag(v2f i) : SV_TARGET
			{
				float2x3 lightingData = lighting(i.worldNormal, i.worldPos, i.normal);
				float3 light = lightingData[0];
				float3 specular = lightingData[1];
				return _OceanColor * float4(light, 1) + float4(specular, 0);
			}
			
			ENDCG

		}
	}
	Fallback "Diffuse"
}