module Math;


public static const float PI             = 3.14159265359;
public static const float TWO_PI         = 6.28318530718;
public static const float FOUR_PI        = 12.56637061436;
public static const float INV_PI         = 0.31830988618;
public static const float INV_TWO_PI     = 0.15915494309;
public static const float INV_FOUR_PI    = 0.07957747155;
public static const float HALF_PI        = 1.57079632679;
public static const float INV_HALF_PI    = 0.636619772367;


// Colors ===========================================================
// http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
[ForceInline]
public static float3 linearToGammaSpace(float3 lin)
{
    return max(1.055 * pow(max(lin, float3(0.0)), float3(0.416666667)) - 0.055, 0.0);
}


// http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
[ForceInline]
public static float3 gammaToLinearSpace(float3 gamma)
{
    return gamma * (gamma * (gamma * 0.305306011 + 0.682171111) + 0.012522878);
}

// ============================================================================

[ForceInline]
public static float linearizeDepth(float depth, float near, float far)
{
    float z = depth * 2.0 - 1.0; // Back to NDC [-1,1] range
    return (2.0 * near * far) / (far + near - z * (far - near));
}

[ForceInline]
public static float getFovFromProjectionMatrix(float4x4 proj)
{
    // proj[1][1] is M11, the Y scale
    // FOV = 2 * atan(1/M11)
    return 2.0 * atan(1.0 / proj[1][1]);
}

// ----------------------------------------------------------------------------

[ForceInline]
public static float3 projectAndDivide(float4x4 matrix, float3 pos) {
    float4 p = mul(matrix, float4(pos, 1.0));
    return p.xyz / p.w;
}

[ForceInline]
public static float3 getScreenPos(float2 tc, float depth) {
	return float3(tc, depth);
}

[ForceInline]
public static float3 getScreenFromViewPos(float3 viewPos, float4x4 projection)
{
	float3 p = projectAndDivide(projection, viewPos);
	return p * 0.5 + 0.5;
}

[ForceInline]
public static float3 getNDCFromScreenPos(float3 screenPos) {
	return screenPos * 2.0 - 1.0;
}

[ForceInline]
public static float3 getViewFromScreenPos(float3 screenPos, float4x4 inverseProjection) {
	return projectAndDivide(inverseProjection, getNDCFromScreenPos(screenPos));
}

[ForceInline]
public static float3 getViewPos(float2 tc, float depth, float4x4 inverseProjection) {
	return getViewFromScreenPos(getScreenPos(tc, depth), inverseProjection);
}

// ----------------------------------------------------------------------------
// Math Utilities
[ForceInline]
public static float rcp(float x) { return (1.0 / (x)); }

// Min/Max of vector components
[ForceInline]
public static float minOf(float2 v) { return min(v.x, v.y); }

[ForceInline]
public static float minOf(float3 v) { return min(v.x, min(v.y, v.z)); }

[ForceInline]
public static float minOf(float4 v) { return min(v.x, min(v.y, min(v.z, v.w))); }

[ForceInline]
public static float maxOf(float2 v) { return max(v.x, v.y); }

[ForceInline]
public static float maxOf(float3 v) { return max(v.x, max(v.y, v.z)); }

[ForceInline]
public static float maxOf(float4 v) { return max(v.x, max(v.y, max(v.z, v.w))); }

// Squared dot product
[ForceInline]
public static float sdot(float2 x) { return dot(x, x); }

[ForceInline]
public static float sdot(float3 x) { return dot(x, x); }

[ForceInline]
public static float sdot(float4 x) { return dot(x, x); }

// Square function
[ForceInline]
public static float sqr(float x) { return x * x; }

[ForceInline]
public static float2 sqr(float2 x) { return x * x; }

[ForceInline]
public static float3 sqr(float3 x) { return x * x; }

[ForceInline]
public static float4 sqr(float4 x) { return x * x; }

// Linear step with saturation
[ForceInline]
public static float linearstep(float a, float b, float x) {
	return saturate((x - a) / (b - a));
}

// Fast approximations for trigonometric functions
[ForceInline]
public static float fastSign(float x) {
	return x >= 0.0 ? 1.0 : -1.0;
}

// Fast acos approximation using polynomial
[ForceInline]
public static float fastAcos(float x) {
	float y = abs(x);
	float p = -0.0187293 * y + 0.0742610;
	p = p * y - 0.2121144;
	p = p * y + 1.5707288;
	p = p * sqrt(1.0 - y);
	return x >= 0.0 ? p : PI - p;
}

// Extract diagonal from matrix
[ForceInline] public static float2 diagonal2(float4x4 m) { return float2(m[0].x, m[1].y); }
[ForceInline] public static float3 diagonal3(float4x4 m) { return float3(m[0].x, m[1].y, m[2].z); }

// Trigonometric utilities
[ForceInline] public static float2 cossin(float x) { return float2(cos(x), sin(x)); }

// ----------------------------------------------------------------------------
// Color Utilities

// Luminance calculation (Rec. 709 coefficients)
[ForceInline]
float luminance(float3 color) {
	return dot(color, float3(0.2126, 0.7152, 0.0722));
}

// ----------------------------------------------------------------------------
// Sampling Utilities

// Sample cosine-weighted hemisphere around a normal
public static float3 SampleCosineHemisphere(float3 normal, float2 xy) {
	float phi = TWO_PI * xy.x;
	float cosTheta = xy.y * 2.0 - 1.0;
	float sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));
	float3 hemisphere = float3(cossin(phi) * sinTheta, cosTheta);

	float3 cosineVector = normalize(normal + hemisphere);
	return cosineVector * fastSign(dot(cosineVector, normal));
}

// ----------------------------------------------------------------------------
// Depth Utilities

// Convert screen-space depth to view-space depth
public static float ScreenToViewDepth(float depth, float4x4 projection) {
	float z = depth * 2.0 - 1.0; // Back to NDC
	return -projection[3].z / (projection[2].z + z);
}

// ----------------------------------------------------------------------------
// Reprojection Utilities

// Reproject current screen position to previous frame using motion
public static float3 Reproject(float2 screenUV, float depth, float4x4 inverseProjection, float4x4 inverseView, float4x4 prevViewProj)
{
	// Get current view space position
	float3 viewPos = getViewPos(screenUV, depth, inverseProjection);

	// Transform to world space
	float4 worldPos = mul(inverseView, float4(viewPos, 1.0));

	// Transform to previous frame's clip space
	float4 prevClip = mul(prevViewProj, worldPos);

	// Perspective divide
	float3 prevNDC = prevClip.xyz / prevClip.w;

	// Convert to screen space [0,1]
	float3 prevScreen = prevNDC * 0.5 + 0.5;

	return prevScreen;
}


// Check if reprojection is valid (no disocclusion)
public static bool IsReprojectionValid(float2 prevUV, float currentDepth, float prevDepth, float3 currentNormal, float3 prevNormal)
{
	// Out of screen
	if (prevUV.x < 0.0 || prevUV.x > 1.0 || prevUV.y < 0.0 || prevUV.y > 1.0)
		return false;

	// Depth discontinuity check
	float depthDiff = abs(currentDepth - prevDepth);
	if (depthDiff > 0.01)
		return false;

	// Normal discontinuity check
	float normalDot = dot(currentNormal, prevNormal);
	if (normalDot < 0.9)
		return false;

	return true;
}


// Normal Mapping ===========================================================

// Apply a tangent-space normal map to a world-space normal.
// If tangents are available (HAS_TANGENTS), constructs a TBN matrix and transforms
// the sampled normal. Otherwise returns the interpolated world normal as-is.
//
// Usage:
//   float3 worldNormal = ApplyNormalMap(normalTex, uv, vNormal, vTangent, vBitangent);
//
// For shaders without normal maps, just use normalize(vNormal) directly.
public static float3 ApplyNormalMap(Texture2D<float4> normalTex, SamplerState state, float2 uv, float3 normal, float3 tangent, float3 bitangent)
{
    float3x3 TBN = float3x3(normalize(tangent), normalize(bitangent), normalize(normal));
    float3 normalTS = normalTex.Sample(state, uv).rgb * 2.0 - 1.0;
    return normalize(mul(TBN, normalTS));
}


// Encode a world-space normal to view-space [0,1] range for the depth-normals pre-pass.
public static float4 EncodeViewNormal(float3 worldNormal, float4x4 viewMatrix)
{
    float3 viewNormal = normalize(mul(float3x3(viewMatrix), worldNormal));
    return float4(viewNormal * 0.5 + 0.5, 1.0);
}
