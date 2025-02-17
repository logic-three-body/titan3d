#include "DX12CommandList.h"
#include "DX12GpuDevice.h"
#include "DX12Shader.h"
#include "DX12Buffer.h"
#include "DX12GpuState.h"
#include "DX12Event.h"
#include "DX12InputAssembly.h"
#include "DX12FrameBuffers.h"
#include "DX12Effect.h"
#include "../NxDrawcall.h"

#include <pix3.h>

#define new VNEW

NS_BEGIN

namespace NxRHI
{
	DX12CommandList::DX12CommandList()
	{
		mContext = nullptr;
	}
	DX12CommandList::~DX12CommandList()
	{
		if (GetDX12Device() == nullptr)
			return;
	}
	bool DX12CommandList::Init(DX12GpuDevice* device)
	{
		mDevice.FromObject(device);
		AutoRef<ID3D12CommandAllocator> tmp;
		auto hr = device->mDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(tmp.GetAddressOf()));
		if (FAILED(hr))
			return false;
		hr = device->mDevice->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, tmp, nullptr, IID_PPV_ARGS(mContext.GetAddressOf()));
		if (FAILED(hr))
			return false;

		FFenceDesc desc;
		desc.InitValue = 0;
		mCommitFence = MakeWeakRef(device->CreateFence(&desc, "Dx12Cmdlist Commit fence"));
		mContext->Close();
		mIsRecording = false;
		return true;
	}
	bool DX12CommandList::BeginCommand()
	{
		if (mIsRecording)
		{
			ASSERT(false);
			mContext->Close();
		}

		//ASSERT(mAllocator == nullptr);
		mAllocator = GetDX12Device()->mCmdAllocatorManager->Alloc(GetDX12Device()->mDevice);
		auto hr = mAllocator->Reset();
		ASSERT(hr == S_OK);
		hr = mContext->Reset(mAllocator, nullptr);
		ASSERT(hr == S_OK);

		mIsRecording = true;
		return true;
	}
	void DX12CommandList::EndCommand()
	{
		if (mIsRecording)
		{
			mContext->Close();
		}
		//mContext->Close();
		mIsRecording = false;
	}
	bool DX12CommandList::BeginPass(IFrameBuffers* fb, const FRenderPassClears* passClears, const char* name)
	{
		ASSERT(mIsRecording);
		mDebugName = name;
		BeginEvent(name);
		mCurrentFrameBuffers = fb;
		mCurRtvs.clear();
		mCurRtvs.resize(fb->mRenderPass->Desc.NumOfMRT);
		for (UINT i = 0; i < fb->mRenderPass->Desc.NumOfMRT; i++)
		{
			auto rtv = fb->mRenderTargets[i].UnsafeConvertTo<DX12RenderTargetView>();
			mCurRtvs[i] = rtv;
			if (rtv != nullptr)
			{
				auto pDxTexture = rtv->GpuResource.UnsafeConvertTo<DX12Texture>();
				pDxTexture->TransitionTo(this, EGpuResourceState::GRS_RenderTarget);
			}
		}
		if (fb->mDepthStencilView != nullptr)
		{
			auto dsv = fb->mDepthStencilView.UnsafeConvertTo<DX12DepthStencilView>();
			if (dsv != nullptr)
			{
				auto pDxTexture = dsv->GpuResource.UnsafeConvertTo<DX12Texture>();
				pDxTexture->TransitionTo(this, EGpuResourceState::GRS_DepthStencil);
			}
		}
		auto dxFB = ((DX12FrameBuffers*)fb);
		auto RTVArraySize = (UINT)dxFB->mDX11RTVArray.size();
		if (RTVArraySize > 0)
		{
			if (dxFB->mDepthStencilView != nullptr)
			{
				//for (size_t i = 0; i < dxFB->mDX11RTVArray.size(); i++)
				//{
				//	auto rtv = dxFB->mDX11RTVArray[i];
				//	//auto rtv = dxFB->mRenderTargets[i].UnsafeConvertTo<DX12RenderTargetView>()->mView->Handle;
				//	mContext->OMSetRenderTargets(1, &rtv, true, &dxFB->mDepthStencilView.UnsafeConvertTo<DX12DepthStencilView>()->mView->Handle);
				//}
				auto handle = dxFB->mDepthStencilView.UnsafeConvertTo<DX12DepthStencilView>()->mView->GetHandle(0);
				mContext->OMSetRenderTargets((UINT)dxFB->mDX11RTVArray.size(), &dxFB->mDX11RTVArray[0], false, &handle);
			}	
			else
			{
				//for (size_t i = 0; i < dxFB->mDX11RTVArray.size(); i++)
				//{
				//	auto rtv = dxFB->mDX11RTVArray[i];
				//	//auto rtv = dxFB->mRenderTargets[i].UnsafeConvertTo<DX12RenderTargetView>()->mView->Handle;
				//	mContext->OMSetRenderTargets(1, &rtv, true, nullptr);
				//}
				mContext->OMSetRenderTargets((UINT)dxFB->mDX11RTVArray.size(), &dxFB->mDX11RTVArray[0], false, nullptr);
			}
		}
		else
		{
			if (dxFB->mDepthStencilView != nullptr)
			{
				auto handle = dxFB->mDepthStencilView.UnsafeConvertTo<DX12DepthStencilView>()->mView->GetHandle(0);
				mContext->OMSetRenderTargets(0, nullptr, true, &handle);
			}
			else
			{
				mContext->OMSetRenderTargets(0, nullptr, true, nullptr);
			}
		}

		if (passClears == nullptr)
		{
			return true;
		}
		auto pRenderPassDesc = &fb->mRenderPass->Desc;

		for (UINT RTVIdx = 0; RTVIdx < RTVArraySize; RTVIdx++)
		{
			UINT flags = ((UINT)passClears->ClearFlags) & (1 << (RTVIdx + 2));
			if (flags != 0)
			{
				if (pRenderPassDesc->AttachmentMRTs[RTVIdx].LoadAction == EFrameBufferLoadAction::LoadActionClear)
				{
					mContext->ClearRenderTargetView(dxFB->mDX11RTVArray[RTVIdx], (const float*)&passClears->ClearColor[RTVIdx], 0, nullptr);
				}
			}
		}

		if (dxFB->mDepthStencilView != nullptr)
		{
			D3D12_CLEAR_FLAGS flag = (D3D12_CLEAR_FLAGS)0;
			UINT clrFlg = (UINT)(passClears->ClearFlags) & ERenderPassClearFlags::CLEAR_DEPTH;
			if (clrFlg != 0 && pRenderPassDesc->AttachmentDepthStencil.LoadAction == EFrameBufferLoadAction::LoadActionClear)
			{
				flag |= D3D12_CLEAR_FLAG_DEPTH;
			}

			clrFlg = (UINT)(passClears->ClearFlags) & ERenderPassClearFlags::CLEAR_STENCIL;
			if (clrFlg != 0 && pRenderPassDesc->AttachmentDepthStencil.StencilLoadAction == EFrameBufferLoadAction::LoadActionClear)
			{
				flag |= D3D12_CLEAR_FLAG_STENCIL;
			}

			if (flag != 0)
			{
				auto handle = dxFB->mDepthStencilView.UnsafeConvertTo<DX12DepthStencilView>()->mView->GetHandle(0);
				mContext->ClearDepthStencilView(handle, flag, passClears->DepthClearValue, passClears->StencilClearValue, 0, nullptr);
			}
		}

		return true;
	}
	void DX12CommandList::SetViewport(UINT Num, const FViewPort* pViewports)
	{
		ASSERT(mIsRecording);
		mContext->RSSetViewports(Num, (const D3D12_VIEWPORT*)pViewports);
	}
	void DX12CommandList::SetScissor(UINT Num, const FScissorRect* pScissor)
	{
		ASSERT(mIsRecording);
		mContext->RSSetScissorRects(Num, (const D3D12_RECT*)pScissor);
	}
	void DX12CommandList::EndPass()
	{
		mCurrentFrameBuffers = nullptr;
		for (auto& i : mCurRtvs)
		{
			auto pDxTexture = i->GpuResource.UnsafeConvertTo<DX12Texture>();
			pDxTexture->TransitionTo(this, EGpuResourceState::GRS_GenericRead);
		}
		mCurRtvs.clear();
		EndEvent();
		ASSERT(mIsRecording);
	}
	void DX12CommandList::BeginEvent(const char* info)
	{
		//ASSERT(mIsRecording);
		//mContext->BeginEvent(1, info, strlen(info));
		PIXBeginEvent(mContext.GetPtr(), 0, info);
	}
	void DX12CommandList::EndEvent()
	{
		//ASSERT(mIsRecording);
		//mContext->EndEvent();
		PIXEndEvent(mContext.GetPtr());
	}
	void DX12CommandList::SetShader(IShader* shader)
	{
		ASSERT(mIsRecording);
		/*switch (shader->Desc->Type)
		{
			case EShaderType::SDT_ComputeShader:
			{
				auto d11CSShader = (DX12Shader*)shader;
				mContext->CSSetShader(d11CSShader->mComputeShader, nullptr, 0);
			}
			break;
			default:
				break;
		}*/
	}
#define DESCRIPTOR_IN_DRAWCALL

	void DX12CommandList::SetCBV(EShaderType type, const FShaderBinder* binder, ICbView* buffer)
	{
		ASSERT(mIsRecording);
		buffer->FlushDirty(this);
		//mContext->SetGraphicsRootConstantBufferView(binder->DescriptorIndex, ((DX12Buffer*)buffer)->mGpuResource->GetGPUVirtualAddress());

#ifndef DESCRIPTOR_IN_DRAWCALL
		buffer->Buffer->TransitionTo(this, EGpuResourceState::GRS_GenericRead);
		auto handle = ((DX12CbView*)buffer)->mView;
		auto device = GetDX12Device()->mDevice;
		if (type == EShaderType::SDT_ComputeShader)
		{
			device->CopyDescriptorsSimple(1, mCurrentComputeSrvTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		}
		else
		{
			device->CopyDescriptorsSimple(1, mCurrentSrvTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		}
#endif
	}
	void DX12CommandList::SetSrv(EShaderType type, const FShaderBinder* binder, ISrView* view)
	{
		ASSERT(mIsRecording);
		view->GetResourceState()->SetAccessTime(VIUnknown::EngineTime);
		ASSERT(view->Buffer->GetGpuResourceState() != EGpuResourceState::GRS_RenderTarget);

		if (type == EShaderType::SDT_PixelShader)
			view->Buffer->TransitionTo(this, (EGpuResourceState)(EGpuResourceState::GRS_SrvPS));
		else
			view->Buffer->TransitionTo(this, (EGpuResourceState)(EGpuResourceState::GRS_GenericRead));
#ifndef DESCRIPTOR_IN_DRAWCALL
		auto handle = ((DX12SrView*)view)->mView;
		auto device = GetDX12Device()->mDevice;
		if (type == EShaderType::SDT_ComputeShader)
		{
			device->CopyDescriptorsSimple(1, mCurrentComputeSrvTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		}
		else
		{
			device->CopyDescriptorsSimple(1, mCurrentSrvTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		}
#endif
	}
	void DX12CommandList::SetUav(EShaderType type, const FShaderBinder* binder, IUaView* view)
	{
		ASSERT(mIsRecording);
		/*auto pAddr = ((ID3D12Resource*)view->Buffer->GetHWBuffer())->GetGPUVirtualAddress();
		mContext->SetGraphicsRootUnorderedAccessView(binder->DescriptorIndex, pAddr);*/
		view->Buffer->TransitionTo(this, EGpuResourceState::GRS_Uav);
#ifndef DESCRIPTOR_IN_DRAWCALL
		auto handle = ((DX12UaView*)view)->mView;
		auto device = GetDX12Device()->mDevice;
		if (type == EShaderType::SDT_ComputeShader)
		{
			device->CopyDescriptorsSimple(1, mCurrentComputeSrvTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		}
		else
		{
			device->CopyDescriptorsSimple(1, mCurrentSrvTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		}
#endif
	}
	void DX12CommandList::SetSampler(EShaderType type, const FShaderBinder* binder, ISampler* sampler)
	{
		ASSERT(mIsRecording);
#ifndef DESCRIPTOR_IN_DRAWCALL
		auto handle = ((DX12Sampler*)sampler)->mView;
		auto device = GetDX12Device()->mDevice;
		if (type == EShaderType::SDT_ComputeShader) 
		{
			device->CopyDescriptorsSimple(1, mCurrentComputeSamplerTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER);
		}
		else 
		{
			device->CopyDescriptorsSimple(1, mCurrentSamplerTable->GetHandle(binder->DescriptorIndex), handle->Handle, D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER);
		}
#endif
	}
	void DX12CommandList::SetVertexBuffer(UINT slot, IVbView* buffer, UINT32 Offset, UINT Stride)
	{
		ASSERT(mIsRecording);
		D3D12_VERTEX_BUFFER_VIEW tmp{};
		tmp.StrideInBytes = Stride;
		
		if (buffer != nullptr)
		{
			tmp.BufferLocation = buffer->Buffer.UnsafeConvertTo<DX12Buffer>()->GetGPUVirtualAddress() + Offset;
			tmp.SizeInBytes = buffer->Desc.Size;
			mContext->IASetVertexBuffers(slot, 1, &tmp);
		}
	}
	void DX12CommandList::SetIndexBuffer(IIbView* buffer, bool IsBit32)
	{
		ASSERT(mIsRecording);
		D3D12_INDEX_BUFFER_VIEW tmp{};
		if (IsBit32)
		{
			tmp.Format = DXGI_FORMAT_R32_UINT;
		}
		else
		{
			tmp.Format = DXGI_FORMAT_R16_UINT;
		}
		if (buffer != nullptr)
		{
			tmp.BufferLocation = buffer->Buffer.UnsafeConvertTo<DX12Buffer>()->GetGPUVirtualAddress();
			tmp.SizeInBytes = buffer->Desc.Size;
		}
		mContext->IASetIndexBuffer(&tmp);
	}
	void DX12CommandList::SetGraphicsPipeline(const IGpuDrawState* drawState)
	{
		ASSERT(mIsRecording);
		mContext->SetPipelineState(((DX12GpuDrawState*)drawState)->mDxState);
	}
	void DX12CommandList::SetComputePipeline(const IComputeEffect* drawState)
	{
		ASSERT(mIsRecording);
		mContext->SetPipelineState(((DX12ComputeEffect*)drawState)->mPipelineState);
	}
	void DX12CommandList::SetInputLayout(IInputLayout* layout)
	{
		ASSERT(mIsRecording);
		//mContext->IASetInputLayout(((DX12InputLayout*)layout)->mLayout);
	}
	static inline D3D12_PRIMITIVE_TOPOLOGY PrimitiveTypeToDX12(EPrimitiveType type, UINT NumPrimitives, UINT* pCount)
	{
		switch (type)
		{
		case EPT_LineStrip:
			*pCount = NumPrimitives;
			return D3D_PRIMITIVE_TOPOLOGY_LINESTRIP;
		case EPT_LineList:
			*pCount = NumPrimitives * 2;
			return D3D_PRIMITIVE_TOPOLOGY_LINELIST;
		case EPT_TriangleList:
			*pCount = NumPrimitives * 3;
			return D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST;
		case EPT_TriangleStrip:
			*pCount = NumPrimitives + 2;
			return D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP;
		}
		return D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST;
	}
	void DX12CommandList::Draw(EPrimitiveType topology, UINT BaseVertex, UINT DrawCount, UINT Instance)
	{
		ASSERT(mIsRecording);
		UINT dpCount = 0;
		mContext->IASetPrimitiveTopology(PrimitiveTypeToDX12(topology, DrawCount, &dpCount));
		mContext->DrawInstanced(dpCount, Instance, BaseVertex, 0);
	}
	void DX12CommandList::DrawIndexed(EPrimitiveType topology, UINT BaseVertex, UINT StartIndex, UINT DrawCount, UINT Instance)
	{
		ASSERT(mIsRecording);
		UINT dpCount = 0;
		mContext->IASetPrimitiveTopology(PrimitiveTypeToDX12(topology, DrawCount, &dpCount));
		mContext->DrawIndexedInstanced(dpCount, Instance, StartIndex, BaseVertex, 0);
	}
	void DX12CommandList::IndirectDrawIndexed(EPrimitiveType topology, IBuffer* indirectArg, UINT indirectArgOffset)
	{
		ASSERT(mIsRecording);
		auto device = (DX12GpuDevice*)mDevice.GetPtr();
		if (device->CmdSigForIndirectDrawIndex == nullptr)
			return;

		UINT dpCount = 0;
		mContext->IASetPrimitiveTopology(PrimitiveTypeToDX12(topology, 0, &dpCount));
		auto dx12Buffer = (DX12Buffer*)indirectArg;
		auto offset = (UINT)dx12Buffer->mGpuMemory->Offset + indirectArgOffset;
		mContext->ExecuteIndirect(device->CmdSigForIndirectDrawIndex, 1, (ID3D12Resource*)dx12Buffer->GetHWBuffer(), offset, nullptr, 0);
	}
	void DX12CommandList::Dispatch(UINT x, UINT y, UINT z)
	{
		ASSERT(mIsRecording);
		mContext->Dispatch(x, y, z);
	}
	void DX12CommandList::IndirectDispatch(IBuffer* indirectArg, UINT indirectArgOffset)
	{
		ASSERT(mIsRecording);
		//mContext->ExecuteIndirect()
		//mContext->DispatchIndirect(((DX12Buffer*)indirectArg)->mBuffer, indirectArgOffset);
		auto device = (DX12GpuDevice*)mDevice.GetPtr();
		if (device->CmdSigForIndirectDispatch == nullptr)
			return;

		auto dx12Buffer = (DX12Buffer*)indirectArg;
		auto offset = (UINT)dx12Buffer->mGpuMemory->Offset + indirectArgOffset;
		auto saved = dx12Buffer->GetGpuResourceState();
		dx12Buffer->TransitionTo(this, EGpuResourceState::GRS_UavIndirect);
		mContext->ExecuteIndirect(device->CmdSigForIndirectDispatch, 1, (ID3D12Resource*)dx12Buffer->GetHWBuffer(), offset, nullptr, 0);
		dx12Buffer->TransitionTo(this, saved);
	}
	void DX12CommandList::SetMemoryBarrier(EPipelineStage srcStage, EPipelineStage dstStage, EBarrierAccess srcAccess, EBarrierAccess dstAccess)
	{
		//D3D12_RESOURCE_BARRIER
		//mContext->ResourceBarrier()
	}
	D3D12_RESOURCE_STATES GpuStateToDX12(EGpuResourceState state)
	{
		switch (state)
		{
		case EngineNS::NxRHI::GRS_Undefine:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_COMMON;
		case EngineNS::NxRHI::GRS_SrvPS:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_GENERIC_READ; //return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
		case EngineNS::NxRHI::GRS_GenericRead:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_GENERIC_READ;
		case EngineNS::NxRHI::GRS_Uav:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_UNORDERED_ACCESS;
		case EngineNS::NxRHI::GRS_UavIndirect:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT;
		case EngineNS::NxRHI::GRS_RenderTarget:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_RENDER_TARGET;
		case EngineNS::NxRHI::GRS_DepthStencil:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_DEPTH_WRITE;
		case EngineNS::NxRHI::GRS_DepthRead:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_DEPTH_READ;
		case EngineNS::NxRHI::GRS_StencilRead:
			ASSERT(false);
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_DEPTH_READ;
		case EngineNS::NxRHI::GRS_DepthStencilRead:
			ASSERT(false);
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_DEPTH_READ;
		case EngineNS::NxRHI::GRS_CopySrc:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_COPY_SOURCE;
		case EngineNS::NxRHI::GRS_CopyDst:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_COPY_DEST;
		case EngineNS::NxRHI::GRS_Present:
			return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_PRESENT;
		default:
			break;
		}
		return D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_GENERIC_READ;
	}
	EGpuResourceState DX12ToGpuState(D3D12_RESOURCE_STATES state)
	{
		switch (state)
		{
		case D3D12_RESOURCE_STATE_COMMON:
			break;
		case D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER:
			break;
		case D3D12_RESOURCE_STATE_INDEX_BUFFER:
			break;
		case D3D12_RESOURCE_STATE_RENDER_TARGET:
			break;
		case D3D12_RESOURCE_STATE_UNORDERED_ACCESS:
			break;
		case D3D12_RESOURCE_STATE_DEPTH_WRITE:
			break;
		case D3D12_RESOURCE_STATE_DEPTH_READ:
			break;
		case D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE:
			break;
		case D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE:
			break;
		case D3D12_RESOURCE_STATE_STREAM_OUT:
			break;
		case D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT:
			break;
		case D3D12_RESOURCE_STATE_COPY_DEST:
			break;
		case D3D12_RESOURCE_STATE_COPY_SOURCE:
			break;
		case D3D12_RESOURCE_STATE_RESOLVE_DEST:
			break;
		case D3D12_RESOURCE_STATE_RESOLVE_SOURCE:
			break;
		case D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE:
			break;
		case D3D12_RESOURCE_STATE_SHADING_RATE_SOURCE:
			break;
		case D3D12_RESOURCE_STATE_GENERIC_READ:
			break;
		case D3D12_RESOURCE_STATE_VIDEO_DECODE_READ:
			break;
		case D3D12_RESOURCE_STATE_VIDEO_DECODE_WRITE:
			break;
		case D3D12_RESOURCE_STATE_VIDEO_PROCESS_READ:
			break;
		case D3D12_RESOURCE_STATE_VIDEO_PROCESS_WRITE:
			break;
		case D3D12_RESOURCE_STATE_VIDEO_ENCODE_READ:
			break;
		case D3D12_RESOURCE_STATE_VIDEO_ENCODE_WRITE:
			break;
		default:
			break;
		}
		return EGpuResourceState::GRS_Undefine;
	}
	void DX12CommandList::SetBufferBarrier(IBuffer* pResource, EPipelineStage srcStage, EPipelineStage dstStage, EGpuResourceState srcAccess, EGpuResourceState dstAccess)
	{
		D3D12_RESOURCE_BARRIER tmp{};
		tmp.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
		tmp.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
		auto pTarGpuResource = (ID3D12Resource*)pResource->GetHWBuffer();
		tmp.Transition.pResource = pTarGpuResource;
		tmp.Transition.StateBefore = GpuStateToDX12(srcAccess);
		tmp.Transition.StateAfter = GpuStateToDX12(dstAccess);
		if (tmp.Transition.StateBefore == tmp.Transition.StateAfter)
			return;
		tmp.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
		mContext->ResourceBarrier(1, &tmp);
	}
	void DX12CommandList::SetTextureBarrier(ITexture* pResource, EPipelineStage srcStage, EPipelineStage dstStage, EGpuResourceState srcAccess, EGpuResourceState dstAccess)
	{
		D3D12_RESOURCE_BARRIER tmp{};
		tmp.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
		tmp.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
		auto pTarGpuResource = (ID3D12Resource*)pResource->GetHWBuffer();
		tmp.Transition.pResource = pTarGpuResource;
		tmp.Transition.StateBefore = GpuStateToDX12(srcAccess);
		tmp.Transition.StateAfter = GpuStateToDX12(dstAccess);
		if (tmp.Transition.StateBefore == tmp.Transition.StateAfter)
			return;
		tmp.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
		mContext->ResourceBarrier(1, &tmp);
	}
	//UINT64 DX12CommandList::SignalFence(IFence* fence, UINT64 value, IEvent* evt)
	//{
	//	ASSERT(mIsRecording);
	//	auto dxFence = ((DX12Fence*)fence);
	//	if (evt != nullptr)
	//	{
	//		dxFence->mFence->SetEventOnCompletion(value, ((DX12Event*)evt)->mHandle);
	//	}
	//	else
	//	{
	//		dxFence->mFence->SetEventOnCompletion(value, dxFence->mEvent->mHandle);
	//	}
	//	//mContext->Signal(dxFence->mFence, value);
	//	ASSERT(false);
	//	return value;
	//}
	//void DX12CommandList::WaitGpuFence(IFence* fence, UINT64 value)
	//{
	//	ASSERT(mIsRecording);
	//	auto dxFence = ((DX12Fence*)fence);
	//	//mContext->Wait(dxFence->mFence, value);
	//	ASSERT(false);
	//}
	void DX12CommandList::CopyBufferRegion(IBuffer* target, UINT64 DstOffset, IBuffer* src, UINT64 SrcOffset, UINT64 Size)
	{
		auto tarSave = target->GetGpuResourceState();
		target->TransitionTo(this, EGpuResourceState::GRS_CopyDst);
		auto srcSave = src->GetGpuResourceState();
		src->TransitionTo(this, EGpuResourceState::GRS_CopySrc);

		if (Size == 0 && DstOffset == 0 && SrcOffset == 0 && target->GetRtti() == src->GetRtti())
		{
			mContext->CopyResource((ID3D12Resource*)target->GetHWBuffer(), (ID3D12Resource*)src->GetHWBuffer());
		}
		else
		{
			if (Size == 0 && target->GetRtti() == GetClassObject<IBuffer>())
			{
				Size = ((DX12Buffer*)target)->Desc.Size;
			}
			mContext->CopyBufferRegion((ID3D12Resource*)target->GetHWBuffer(), DstOffset, (ID3D12Resource*)src->GetHWBuffer(), SrcOffset, Size);
		}

		target->TransitionTo(this, tarSave);
		src->TransitionTo(this, srcSave);
	}
	void DX12CommandList::CopyTextureRegion(ITexture* target, UINT tarSubRes, UINT DstX, UINT DstY, UINT DstZ, ITexture* source, UINT srcSubRes, const FSubresourceBox* box)
	{
		auto tarSave = target->GetGpuResourceState();
		target->TransitionTo(this, EGpuResourceState::GRS_CopyDst);
		auto srcSave = source->GetGpuResourceState();

		source->TransitionTo(this, EGpuResourceState::GRS_CopySrc);
		D3D12_TEXTURE_COPY_LOCATION dst{};
		dst.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
		dst.SubresourceIndex = tarSubRes;
		dst.pResource = (ID3D12Resource*)target->GetHWBuffer();
		D3D12_TEXTURE_COPY_LOCATION src{};
		src.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
		src.SubresourceIndex = srcSubRes;
		src.pResource = (ID3D12Resource*)source->GetHWBuffer();
		mContext->CopyTextureRegion(&dst, DstX, DstY, DstZ, &src, (D3D12_BOX*)box);
		
		target->TransitionTo(this, tarSave);
		source->TransitionTo(this, srcSave);
	}
	void DX12CommandList::CopyBufferToTexture(ITexture* target, UINT subRes, IBuffer* source, const FSubResourceFootPrint* footprint)
	{
		auto tarSave = target->GetGpuResourceState();
		target->TransitionTo(this, EGpuResourceState::GRS_CopySrc);
		auto srcSave = source->GetGpuResourceState();

		source->TransitionTo(this, EGpuResourceState::GRS_CopySrc);

		D3D12_TEXTURE_COPY_LOCATION dst = {};
		dst.pResource = (ID3D12Resource*)target->GetHWBuffer();
		dst.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
		dst.SubresourceIndex = subRes;

		D3D12_TEXTURE_COPY_LOCATION src{};
		src.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
		src.pResource = (ID3D12Resource*)target->GetHWBuffer();
		src.PlacedFootprint.Footprint.Format = FormatToDX12Format(footprint->Format);
		src.PlacedFootprint.Footprint.Width = footprint->Width;
		src.PlacedFootprint.Footprint.Height = footprint->Height;
		src.PlacedFootprint.Footprint.Depth = footprint->Depth;
		src.PlacedFootprint.Footprint.RowPitch = footprint->RowPitch;
		
		D3D12_BOX box;
		box.left = footprint->X;
		box.top = footprint->Y;
		box.front = footprint->Z;
		box.right = box.left + footprint->Width;
		box.bottom = box.top + footprint->Height;
		box.back = box.front + footprint->Depth;
		mContext->CopyTextureRegion(&dst, footprint->X, footprint->Y, footprint->Z, &src, &box);

		target->TransitionTo(this, tarSave);
		source->TransitionTo(this, srcSave);
	}
	void DX12CommandList::CopyTextureToBuffer(IBuffer* target, const FSubResourceFootPrint* footprint, ITexture* source, UINT subRes)
	{
		auto tarSave = target->GetGpuResourceState();
		target->TransitionTo(this, EGpuResourceState::GRS_CopySrc);
		auto srcSave = source->GetGpuResourceState();

		source->TransitionTo(this, EGpuResourceState::GRS_CopySrc);
		D3D12_TEXTURE_COPY_LOCATION dst{};
		dst.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
		dst.pResource = (ID3D12Resource*)target->GetHWBuffer();
		dst.PlacedFootprint.Footprint.Format = FormatToDX12Format(footprint->Format);
		dst.PlacedFootprint.Footprint.Width = footprint->Width;
		dst.PlacedFootprint.Footprint.Height = footprint->Height;
		dst.PlacedFootprint.Footprint.Depth = footprint->Depth;
		dst.PlacedFootprint.Footprint.RowPitch = footprint->RowPitch;		
		D3D12_TEXTURE_COPY_LOCATION src{};
		src.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
		src.SubresourceIndex = subRes;
		src.pResource = (ID3D12Resource*)source->GetHWBuffer();
		D3D12_BOX box;
		box.left = footprint->X;
		box.top = footprint->Y;
		box.front = footprint->Z;
		box.right = box.left + footprint->Width;
		box.bottom = box.top + footprint->Height;
		box.back = box.front + footprint->Depth;
		mContext->CopyTextureRegion(&dst, footprint->X, footprint->Y, footprint->Z, &src, &box);

		target->TransitionTo(this, tarSave);
		source->TransitionTo(this, srcSave);

	}
	void DX12CommandList::Flush()
	{
		ASSERT(mIsRecording);
		ASSERT(false);
		//mContext->Flush();
	}
	void DX12CommandList::Commit(DX12CmdQueue* cmdQueue)
	{
		//ASSERT(mIsRecording == false);
		if (mAllocator == nullptr)
			return;
		//BeginEvent(mDebugName.c_str());
		cmdQueue->mCmdQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&mContext);
		auto targetValue = mCommitFence->GetAspectValue() + 1;
		cmdQueue->SignalFence(mCommitFence, targetValue);
		//EndEvent();
		
		GetDX12Device()->mCmdAllocatorManager->Free(mAllocator, targetValue, mCommitFence);
		mAllocator = nullptr;
	}
}

NS_END