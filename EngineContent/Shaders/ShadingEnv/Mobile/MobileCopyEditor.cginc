#ifndef _MOBILE_COPY_
#define _MOBILE_COPY_

#include "../../Inc/VertexLayout.cginc"
#include "../../Inc/LightCommon.cginc"
#include "../../Inc/Math.cginc"
#include "../../Inc/ShadowCommon.cginc"
#include "../../Inc/FogCommon.cginc"
#include "../../Inc/MixUtility.cginc"
#include "../../Inc/SysFunction.cginc"
#include "../../Inc/PostEffectCommon.cginc"
#include "../../Inc/GpuSceneCommon.cginc"

#include "MdfQueue"

#define FXAA_GREEN_AS_LUMA		1
#define FXAA_QUALITY__PRESET		10
#define FXAA_HLSL_4 1

#include "../../Inc/FXAAMobile.cginc"

Texture2D gBaseSceneView DX_AUTOBIND;
SamplerState Samp_gBaseSceneView DX_AUTOBIND;

Texture2D gBloomTex DX_AUTOBIND;
SamplerState Samp_gBloomTex DX_AUTOBIND;

Texture2D gPickedTex DX_AUTOBIND;
SamplerState Samp_gPickedTex DX_AUTOBIND;

Texture2D GVignette DX_AUTOBIND;
SamplerState Samp_GVignette DX_AUTOBIND;

Texture2D gSunShaft DX_AUTOBIND;
SamplerState Samp_gSunShaft DX_AUTOBIND;

Texture2D gMobileAoTex DX_AUTOBIND;
SamplerState Samp_gMobileAoTex DX_AUTOBIND;

PS_INPUT VS_Main(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;

	output.vPosition = float4(input.vPosition.xyz, 1.0f);
	output.vUV = input.vUV;
#if RHI_TYPE == RHI_GL
	output.vUV.y = 1 - input.vUV.y;
#endif
	output.vLightMap.xy = gSunPosNDC.xy - input.vPosition.xy;
	output.vLightMap.z = gSunPosNDC.z;
	output.vLightMap.w = gSunPosNDC.w;
	output.vLightMap.xy = CalcVignetteVS(output.vPosition.xy);

	return output;
}

struct PS_OUTPUT
{
	float4 RT0 : SV_Target0;
};

PS_OUTPUT PS_Main(PS_INPUT input)
{
	PS_OUTPUT output = (PS_OUTPUT)0;

	FxaaTex TempTex;
	TempTex.smpl = Samp_gBaseSceneView;
	TempTex.tex = gBaseSceneView;
#if ENV_DISABLE_FSAA == 1
	half4 TexelAA = gBaseSceneView.Sample(Samp_gBaseSceneView, input.vUV.xy);
#else
	half4 TexelAA = FxaaMobilePS(
		input.vUV,																//FxaaFloat2 pos,
		TempTex,																//FxaaTex tex,
		gViewportSizeAndRcp.zw,															//FxaaFloat2 fxaaQualityRcpFrame,
		1.0,																			//highest value,FxaaFloat fxaaQualitySubpix,
		0.166,																		//default value,FxaaFloat fxaaQualityEdgeThreshold,
		0.0833																		//default value,FxaaFloat fxaaQualityEdgeThresholdMin,
	);
#endif

#if ENV_DISABLE_AO == 1
	TexelAA.b = TexelAA.b / AO_M;
	half4 Color_Depth = TexelAA.rgba;
#else
	TexelAA.b = TexelAA.b / AO_M;
	half MobileAo = (half)gMobileAoTex.Sample(Samp_gMobileAoTex, input.vUV.xy).r;
	half4 Color_Depth = (half4)gBaseSceneView.Sample(Samp_gBaseSceneView, input.vUV.xy).rgba;
	half AoOffset = 1.0h - frac(Color_Depth.b);
	MobileAo = min(MobileAo + AoOffset, 1.0h);
	//MobileAo = min(Pow2(MobileAo) + AoOffset, 1.0h);
	//MobileAo = min(Pow4(MobileAo) + AoOffset, 1.0h);
	
	TexelAA.rgb = TexelAA.rgb * MobileAo;
#endif

	half3 Color = 0.0h;
	
#if ENV_DISABLE_SUNSHAFT == 1
	Color = TexelAA.rgb;
#else
	//sun shaft
	half3 DirLightColor = (half3)gDirLightColor_Intensity.rgb;
	half4 SunShaftParam = (half4)input.vLightMap;
	if (SunShaftParam.w > 0)
	{
		half2 SunShaftM = (half2)gSunShaft.Sample(Samp_gSunShaft, input.vUV.xy).rg;
		half NdcAtten = dot(SunShaftParam.xy, SunShaftParam.xy);
		half SunShaftAtten = exp2(-8.0h * min(1.0h, NdcAtten * 0.125h));
		Color = SunShaftM.r * DirLightColor * half3(1.0h, 1.0h, 0.8h) * SunShaftAtten;
		Color = Color * SunShaftParam.z + TexelAA.rgb;

		half SunBloomAtten = 1.0h - min(NdcAtten, 0.625h) * 1.6h;
		half SunBloomAtten2 = SunBloomAtten * SunBloomAtten;
		half SunBloomAtten8 = Pow4(SunBloomAtten2);
		half3 SunBloomColor = SunShaftM.g * DirLightColor * half3(SunBloomAtten2, SunBloomAtten8, Pow2(SunBloomAtten8));
		Color = (half3(1.0h, 1.0h, 1.0h) - SunBloomColor) * Color + SunBloomColor * 1.25h;
	}
	else
	{
		Color = TexelAA.rgb;
	}
#endif
	
#if ENV_DISABLE_BLOOM == 1
#else
	//bloom effect
	half3 BloomColor = gBloomTex.Sample(Samp_gBloomTex, input.vUV.xy).rgb;
	Color = lerp(Color.rgb, BloomColor, 0.05h);
#endif

#if ENV_DISABLE_HDR == 1
	half VignetteWeight = 1.0h;
#else
	half LumHdr = CalcLuminanceYCbCr(Color);
	half VignetteWeight = max(1.0h - LumHdr, 0.0h);
	//half VignetteMask =1.0h - CalcVignettePS(input.vLightMap.xy, 0.5h);
	half VignetteMask = 1.0h - GVignette.Sample(Samp_GVignette, input.vUV.xy).r;
	Color = (1.0h - VignetteMask * VignetteWeight) * Color;

	Color = ACESMobile_HQ(Color);
#endif

	//pick effect;
	half LinearDepth = Color_Depth.a;
	half2 PickedData = gPickedTex.Sample(Samp_gPickedTex, input.vUV.xy).rg;
	half3 PickedEdgeColor = 0.0h;
	half PickedContrast = 0.0h;
	if (VignetteWeight < 0.3h)
	{
		PickedContrast = 0.3h;
	}
	else
	{
		PickedContrast = 1.0h;
	}
	
	if (PickedData.g - LinearDepth > 0.0h)
	{	
		PickedEdgeColor = half3(1.0h, 0.0h, 0.0h) * PickedContrast;
	}
	else
	{
		PickedEdgeColor = half3(0.0h, 1.0h, 0.0h) * PickedContrast;
	}

	Color = lerp(Color, PickedEdgeColor, PickedData.r);

	//Color = half4(input.vUV.xy, 0.0h, 1.0h);

	output.RT0.rgb = Linear2sRGB(Color);
	
	output.RT0.a = 1.0h;

	return output;
}

#endif