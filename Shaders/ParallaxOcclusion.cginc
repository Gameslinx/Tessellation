#pragma once

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