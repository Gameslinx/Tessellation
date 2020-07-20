#pragma once

// Shamelessly derived from: 
// https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
// License: https://www.gamedev.net/resources/_/gdnethelp/gamedevnet-open-license-r2956

void parallax_vert(	//For positive world X and Z
	float4 vertex,
	float3 normal,
	float4 tangent,
	out float3 eye,
	out float sampleRatio
) {
	float3 binormal = cross(normal, tangent.xyz) * tangent.w;
	float3 EyePosition = _WorldSpaceCameraPos;

	//Corrected the calculation from the above website
	float4 localCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
	float3 eyeLocal = vertex - localCameraPos;
	float4 eyeGlobal = mul(float4(eyeLocal, 1), unity_ObjectToWorld);
	float3 E = eyeGlobal.xyz;

	float3x3 objectToTangent = float3x3(
		tangent.xyz,
		cross(normal, tangent.xyz) * tangent.w,
		normal
		);

	eye = mul(objectToTangent, ObjSpaceViewDir(vertex));

	sampleRatio = 1 - dot(normalize(E), -normal);
}
void reverse_parallax_vert(	//For negative X and Z
	float4 vertex,
	float3 normal,
	float4 negativeTangent,
	out float3 reverseEye,
	out float reverseSampleRatio
) {
	float3 binormal = cross(normal, negativeTangent.xyz) * negativeTangent.w;
	float3 EyePosition = _WorldSpaceCameraPos;

	float4 localCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
	float3 eyeLocal = vertex - localCameraPos;
	float4 eyeGlobal = mul(float4(eyeLocal, 1), unity_ObjectToWorld);
	float3 E = eyeGlobal.xyz;

	float3x3 objectToTangent = float3x3(
		negativeTangent.xyz,
		cross(normal, negativeTangent.xyz) * negativeTangent.w,
		normal
		);

	reverseEye = mul(objectToTangent, ObjSpaceViewDir(vertex));

	reverseSampleRatio = 1 - dot(normalize(E), -normal);
}

float2 parallax_offset (	//For positive world X and Z
	float fHeightMapScale,
	float3 eye,
	float sampleRatio,
	float2 texcoord,
	sampler2D heightMap,
	int nMinSamples,
	int nMaxSamples
) {

	float fParallaxLimit = -length( eye.xy ) / eye.z;
	fParallaxLimit *= fHeightMapScale;
	
	float2 vOffsetDir = normalize( eye.xy );
	float2 vMaxOffset = vOffsetDir * fParallaxLimit;
	
	int nNumSamples = (int)lerp( nMinSamples, nMaxSamples, saturate(sampleRatio) );
	
	float fStepSize = 1.0 / (float)nNumSamples;
	
	float2 dx = ddx( texcoord );
	float2 dy = ddy( texcoord );
	
	float fCurrRayHeight = 1.0;
	float2 vCurrOffset = float2( 0, 0 );
	float2 vLastOffset = float2( 0, 0 );

	float fLastSampledHeight = 1;
	float fCurrSampledHeight = 1;

	int nCurrSample = 0;
	
	while ( nCurrSample < nNumSamples )
	{
		  fCurrSampledHeight = tex2Dgrad(heightMap, texcoord + vCurrOffset, dx, dy ).r;
		  if ( fCurrSampledHeight > fCurrRayHeight )
		  {
				float delta1 = fCurrSampledHeight - fCurrRayHeight;
				float delta2 = ( fCurrRayHeight + fStepSize ) - fLastSampledHeight;

				float ratio = delta1/(delta1+delta2);

				vCurrOffset = (ratio) * vLastOffset + (1.0-ratio) * vCurrOffset;

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
float2 reverse_parallax_offset(	//For negative world X and Z
	float fHeightMapScale,
	float3 eye,
	float sampleRatio,
	float2 texcoord,
	sampler2D heightMap,
	int nMinSamples,
	int nMaxSamples
) {

	float fParallaxLimit = -length(eye.xy) / eye.z;
	fParallaxLimit *= fHeightMapScale;

	float2 vOffsetDir = normalize(eye.xy);
	float2 vMaxOffset = vOffsetDir * fParallaxLimit;

	int nNumSamples = (int)lerp(nMinSamples, nMaxSamples, saturate(sampleRatio));

	float fStepSize = 1.0 / (float)nNumSamples;

	float2 dx = ddx(texcoord);
	float2 dy = ddy(texcoord);

	float fCurrRayHeight = 1.0;
	float2 vCurrOffset = float2(0, 0);
	float2 vLastOffset = float2(0, 0);

	float fLastSampledHeight = 1;
	float fCurrSampledHeight = 1;

	int nCurrSample = 0;

	while (nCurrSample < nNumSamples)
	{
		fCurrSampledHeight = tex2Dgrad(heightMap, texcoord + vCurrOffset, dx, dy).r;
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