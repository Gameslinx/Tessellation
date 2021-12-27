float _TessellationMax;
float _TessellationEdgeLength;
float _TessellationRange;

struct vertexInput
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
};

struct vertexOutput
{
	float4 vertex : SV_POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
};

struct TessellationFactors
{
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

vertexInput vert(vertexInput v)
{
	return v;
}

vertexOutput tessVert(vertexInput v)
{
	vertexOutput o;
	// Note that the vertex is NOT transformed to clip
	// space here; this is done in the grass geometry shader.
	o.vertex = v.vertex;
	o.normal = v.normal;
	o.tangent = v.tangent;
	return o;
}

float TessellationEdgeFactor(vertexOutput cp0, vertexOutput cp1)
{
	float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
	float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
	float edgeLength = distance(p0, p1);
	float3 edgeCenter = (p0 + p1) * 0.5;

	float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);
	if (viewDistance < _TessellationRange)
	{
		return min(_TessellationMax, max((edgeLength * pow(_ScreenParams.y / (_TessellationEdgeLength * viewDistance), 2)), 1));
	}
	else
	{
		return 1;
	}
}
TessellationFactors patchConstantFunction(InputPatch<vertexInput, 3> patch)
{
	TessellationFactors f;

	float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex).xyz;
	float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex).xyz;
	float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex).xyz;
	f.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
	f.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
	f.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
	f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * 0.3333;


	//f.edge[0] = _TessellationUniform;
	//f.edge[1] = _TessellationUniform;
	//f.edge[2] = _TessellationUniform;
	//f.inside = _TessellationUniform;
	return f;
}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("integer")]
[UNITY_patchconstantfunc("patchConstantFunction")]
vertexInput hull(InputPatch<vertexInput, 3> patch, uint id : SV_OutputControlPointID)
{
	return patch[id];
}

[UNITY_domain("tri")]
vertexOutput domain(TessellationFactors factors, OutputPatch<vertexInput, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
	vertexInput v;

#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) v.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
		MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
		MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)

		return tessVert(v);
}