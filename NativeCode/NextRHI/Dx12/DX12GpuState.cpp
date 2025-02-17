#include "DX12GpuState.h"
#include "DX12GpuDevice.h"
#include "DX12FrameBuffers.h"
#include "DX12Effect.h"
#include "DX12InputAssembly.h"
#include "../NxEffect.h"

#define new VNEW

NS_BEGIN

namespace NxRHI
{
	static D3D12_FILTER FilterToDX(ESamplerFilter fiter)
	{
		return (D3D12_FILTER)fiter;
	}
	static D3D12_COMPARISON_FUNC CmpModeToDX(EComparisionMode cmp)
	{
		switch (cmp)
		{
		case CMP_NEVER:
			return D3D12_COMPARISON_FUNC_NEVER;
		case CMP_LESS:
			return D3D12_COMPARISON_FUNC_LESS;
		case CMP_EQUAL:
			return D3D12_COMPARISON_FUNC_EQUAL;
		case CMP_LESS_EQUAL:
			return D3D12_COMPARISON_FUNC_LESS_EQUAL;
		case CMP_GREATER:
			return D3D12_COMPARISON_FUNC_GREATER;
		case CMP_NOT_EQUAL:
			return D3D12_COMPARISON_FUNC_NOT_EQUAL;
		case CMP_GREATER_EQUAL:
			return D3D12_COMPARISON_FUNC_GREATER_EQUAL;
		case CMP_ALWAYS:
			return D3D12_COMPARISON_FUNC_ALWAYS;
		}
		return D3D12_COMPARISON_FUNC_NEVER;
	}
	static D3D12_TEXTURE_ADDRESS_MODE AddressModeToDX(EAddressMode mode)
	{
		switch (mode)
		{
		case ADM_WRAP:
			return D3D12_TEXTURE_ADDRESS_MODE_WRAP;
		case ADM_MIRROR:
			return D3D12_TEXTURE_ADDRESS_MODE_MIRROR;
		case ADM_CLAMP:
			return D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
		case ADM_BORDER:
			return D3D12_TEXTURE_ADDRESS_MODE_BORDER;
		case ADM_MIRROR_ONCE:
			return D3D12_TEXTURE_ADDRESS_MODE_MIRROR_ONCE;
		}
		return D3D12_TEXTURE_ADDRESS_MODE_WRAP;
	}
	DX12Sampler::DX12Sampler()
	{
	}
	DX12Sampler::~DX12Sampler()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;

		if (mView != nullptr)
		{
			device->DelayDestroy(mView);
			mView = nullptr;
		}
	}
	bool DX12Sampler::Init(DX12GpuDevice* device, const FSamplerDesc& desc)
	{
		Desc = desc;
		mView = device->mSamplerAllocHeapManager->Alloc<DX12DescriptorSetPagedObject>();
		mDeviceRef.FromObject(device);

		D3D12_SAMPLER_DESC				mDxDesc;
		mDxDesc.Filter = FilterToDX(desc.Filter);
		mDxDesc.AddressU = AddressModeToDX(desc.AddressU);
		mDxDesc.AddressV = AddressModeToDX(desc.AddressV);
		mDxDesc.AddressW = AddressModeToDX(desc.AddressW);
		mDxDesc.ComparisonFunc = CmpModeToDX(desc.CmpMode);
		mDxDesc.MinLOD = desc.MinLOD;
		mDxDesc.MaxLOD = desc.MaxLOD;
		memcpy(mDxDesc.BorderColor, desc.BorderColor, sizeof(desc.BorderColor));
		mDxDesc.MaxAnisotropy = desc.MaxAnisotropy;
		mDxDesc.MipLODBias = desc.MipLODBias;

		device->mDevice->CreateSampler(&mDxDesc, mView->GetHandle(0));
		return true;
	}

	DX12GpuPipeline::DX12GpuPipeline()
	{

	}
	DX12GpuPipeline::~DX12GpuPipeline()
	{
	}
	bool DX12GpuPipeline::Init(DX12GpuDevice* device, const FGpuPipelineDesc& desc)
	{
		Desc = desc;
		
		mRasterState.FillMode = (D3D12_FILL_MODE)Desc.Rasterizer.FillMode;
		mRasterState.CullMode = (D3D12_CULL_MODE)Desc.Rasterizer.CullMode;
		mRasterState.FrontCounterClockwise = Desc.Rasterizer.FrontCounterClockwise;
		mRasterState.DepthBias = Desc.Rasterizer.DepthBias;
		mRasterState.DepthBiasClamp = Desc.Rasterizer.DepthBiasClamp;
		mRasterState.SlopeScaledDepthBias = Desc.Rasterizer.SlopeScaledDepthBias;
		mRasterState.DepthClipEnable = Desc.Rasterizer.DepthClipEnable;
		//mRasterState.ScissorEnable = Desc.Rasterizer.ScissorEnable;
		mRasterState.MultisampleEnable = Desc.Rasterizer.MultisampleEnable;
		mRasterState.AntialiasedLineEnable = Desc.Rasterizer.AntialiasedLineEnable;
		mRasterState.ForcedSampleCount = 0;
		mRasterState.ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;

		mBlendState.AlphaToCoverageEnable = Desc.Blend.AlphaToCoverageEnable;
		mBlendState.IndependentBlendEnable = Desc.Blend.IndependentBlendEnable;
		for (int i = 0; i < 8; i++)
		{
			mBlendState.RenderTarget[i].BlendEnable = Desc.Blend.RenderTarget[i].BlendEnable;
			mBlendState.RenderTarget[i].LogicOpEnable = FALSE;
			mBlendState.RenderTarget[i].SrcBlend = (D3D12_BLEND)Desc.Blend.RenderTarget[i].SrcBlend;
			mBlendState.RenderTarget[i].DestBlend = (D3D12_BLEND)Desc.Blend.RenderTarget[i].DestBlend;
			mBlendState.RenderTarget[i].BlendOp = (D3D12_BLEND_OP)Desc.Blend.RenderTarget[i].BlendOp;
			mBlendState.RenderTarget[i].SrcBlendAlpha = (D3D12_BLEND)Desc.Blend.RenderTarget[i].SrcBlendAlpha;
			mBlendState.RenderTarget[i].DestBlendAlpha = (D3D12_BLEND)Desc.Blend.RenderTarget[i].DestBlendAlpha;
			mBlendState.RenderTarget[i].BlendOpAlpha = (D3D12_BLEND_OP)Desc.Blend.RenderTarget[i].BlendOpAlpha;
			mBlendState.RenderTarget[i].LogicOp = D3D12_LOGIC_OP_NOOP;
			mBlendState.RenderTarget[i].RenderTargetWriteMask = Desc.Blend.RenderTarget[i].RenderTargetWriteMask;
		}

		mDepthStencilState.DepthEnable = Desc.DepthStencil.DepthEnable;
		mDepthStencilState.DepthWriteMask = (D3D12_DEPTH_WRITE_MASK)Desc.DepthStencil.DepthWriteMask;
		mDepthStencilState.DepthFunc = CmpModeToDX(Desc.DepthStencil.DepthFunc);
		mDepthStencilState.StencilEnable = Desc.DepthStencil.StencilEnable;
		mDepthStencilState.StencilReadMask = Desc.DepthStencil.StencilReadMask;
		mDepthStencilState.StencilWriteMask = Desc.DepthStencil.StencilWriteMask;
		mDepthStencilState.FrontFace = *(D3D12_DEPTH_STENCILOP_DESC*)(&Desc.DepthStencil.FrontFace);
		mDepthStencilState.BackFace = *(D3D12_DEPTH_STENCILOP_DESC*)(&Desc.DepthStencil.BackFace);

		return true;
	}
	static inline D3D12_PRIMITIVE_TOPOLOGY_TYPE PrimitiveTypeToDX12(EPrimitiveType type)
	{
		switch (type)
		{
		case EPT_LineStrip:
			return D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE;
		case EPT_LineList:
			return D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE;
		case EPT_TriangleList:
			return D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
		case EPT_TriangleStrip:
			return D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
		}
		return D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
	}
	bool DX12GpuDrawState::BuildState(IGpuDevice* device)
	{
		auto pDx12 = this->Pipeline.UnsafeConvertTo<DX12GpuPipeline>();
		auto pEffect = ShaderEffect.UnsafeConvertTo<DX12GraphicsEffect>();
		auto pInputLayout = pEffect->mInputLayout.UnsafeConvertTo<DX12InputLayout>();
		std::vector<D3D12_INPUT_ELEMENT_DESC> mDx12Elements;
		pInputLayout->GetDX12Elements(mDx12Elements);

		D3D12_GRAPHICS_PIPELINE_STATE_DESC desc{};
		desc.InputLayout = { &mDx12Elements[0], (UINT)mDx12Elements.size()};
		desc.pRootSignature = pEffect->mSignature;
		desc.RasterizerState = pDx12->mRasterState; 
		desc.DepthStencilState = pDx12->mDepthStencilState;
		desc.BlendState = pDx12->mBlendState;
		desc.SampleMask = UINT_MAX;
		desc.NumRenderTargets = RenderPass->Desc.NumOfMRT;
		desc.DSVFormat = FormatToDX12Format(RenderPass->Desc.AttachmentDepthStencil.Format);
		//ASSERT(desc.DSVFormat != DXGI_FORMAT_UNKNOWN);
		desc.SampleDesc.Count = 1;
		desc.SampleDesc.Quality = 0;
		desc.PrimitiveTopologyType = PrimitiveTypeToDX12(TopologyType);
		for (UINT i = 0; i < RenderPass->Desc.NumOfMRT; i++)
		{
			desc.RTVFormats[i] = FormatToDX12Format(RenderPass->Desc.AttachmentMRTs[i].Format);
			ASSERT(desc.RTVFormats[i] != DXGI_FORMAT_UNKNOWN);
		}
		
		desc.VS =
		{
			&ShaderEffect->mVertexShader->Desc->DxIL[0],
			ShaderEffect->mVertexShader->Desc->DxIL.size()
		};
		desc.PS =
		{
			&ShaderEffect->mPixelShader->Desc->DxIL[0],
			ShaderEffect->mPixelShader->Desc->DxIL.size()
		};
		ID3D12PipelineState* pState;
		if (S_OK != ((DX12GpuDevice*)device)->mDevice->CreateGraphicsPipelineState(&desc, IID_PPV_ARGS(&pState)))
		{
			return false;
		}
		mDxState = pState;
		return true;
	}
}

NS_END