#include "DX12Buffer.h"
#include "DX12CommandList.h"
#include "DX12GpuDevice.h"
#include "DX12Event.h"
#include "DX12Effect.h"

#define new VNEW

NS_BEGIN

namespace NxRHI
{
	template<>
	struct AuxGpuResourceDestroyer<AutoRef<FGpuMemory>>
	{
		static void Destroy(AutoRef<FGpuMemory> obj, IGpuDevice* device1)
		{
			obj->FreeMemory();
		}
	};
	template<>
	struct AuxGpuResourceDestroyer<AutoRef<ID3D12Resource>>
	{
		static void Destroy(AutoRef<ID3D12Resource> obj, IGpuDevice* device1)
		{
			
		}
	};
	template<>
	struct AuxGpuResourceDestroyer<std::vector<AutoRef<ID3D12Resource>>>
	{
		static void Destroy(std::vector<AutoRef<ID3D12Resource>> obj, IGpuDevice* device1)
		{
			
		}
	}; 
	
	DX12Buffer::DX12Buffer()
	{
		
	}

	DX12Buffer::~DX12Buffer()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;

		device->DelayDestroy(mGpuMemory);
		mGpuMemory = nullptr;
	}

	AutoRef<ID3D12Resource> CreateUploadBuffer(DX12GpuDevice* device, void* pData, UINT64 totalSize, UINT size)
	{
		D3D12_HEAP_PROPERTIES properties{};
		properties.Type = D3D12_HEAP_TYPE_UPLOAD;
		properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		properties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
		D3D12_RESOURCE_DESC resDesc{};
		resDesc.SampleDesc.Count = 1;
		resDesc.SampleDesc.Quality = 0;
		resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
		resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
		resDesc.MipLevels = 1;
		resDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
		resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
		resDesc.Alignment = 0;
		resDesc.Width = totalSize;
		resDesc.Height = 1;
		resDesc.DepthOrArraySize = 1;
		resDesc.MipLevels = 1;
		resDesc.Format = DXGI_FORMAT_UNKNOWN;
		resDesc.SampleDesc.Count = 1;
		resDesc.SampleDesc.Quality = 0;
		resDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;

		AutoRef<ID3D12Resource> uploadBuffer;
		auto hr = device->mDevice->CreateCommittedResource(&properties, D3D12_HEAP_FLAG_NONE, &resDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ, NULL, IID_PPV_ARGS(uploadBuffer.GetAddressOf()));
		ASSERT(hr == S_OK);
		BYTE* mapped = NULL;
		D3D12_RANGE range = { 0, size };
		hr = uploadBuffer->Map(0, nullptr, (void**)&mapped);
		if (hr == S_OK)
		{
			BYTE* pCopyTar = mapped;
			BYTE* pCopySrc = (BYTE*)pData;
			memcpy(pCopyTar, pCopySrc, size);
			uploadBuffer->Unmap(0, &range);
		}

		return uploadBuffer;
	}
	bool DX12Buffer::Init(DX12GpuDevice* device, const FBufferDesc& desc)
	{
		Desc = desc;
		Desc.InitData = nullptr;
		mDeviceRef.FromObject(device);
		
		D3D12_RESOURCE_STATES resState = D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_COMMON;
		GpuState = EGpuResourceState::GRS_GenericRead;
		D3D12_HEAP_PROPERTIES properties{};
		properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY::D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		switch (desc.Usage)
		{
			case USAGE_IMMUTABLE:
			case USAGE_DEFAULT:
				properties.Type = D3D12_HEAP_TYPE_DEFAULT;
				break;
			case USAGE_DYNAMIC:
				properties.Type = D3D12_HEAP_TYPE_UPLOAD;
				resState = D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_GENERIC_READ;
				GpuState = EGpuResourceState::GRS_GenericRead;
				break;
			case USAGE_STAGING:
				properties.Type = D3D12_HEAP_TYPE_READBACK;
				break;
		}
		
		D3D12_RESOURCE_DESC resDesc{};
		resDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
		//resDesc.Width = desc.Size;
		resDesc.Width = desc.Size;
		resDesc.Height = 1;
		resDesc.DepthOrArraySize = 1;
		resDesc.MipLevels = 1;
		resDesc.SampleDesc.Count = 1;
		resDesc.SampleDesc.Quality = 0;
		resDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
		resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
		resDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

		auto pAlignment = device->GetGpuResourceAlignment();
		if (desc.Type & EBufferType::BFT_CBuffer)
		{
			//resDesc.Alignment = pAlignment->CBufferAlignment;
			if (resDesc.Width % pAlignment->CBufferAlignment)
			{
				resDesc.Width = (resDesc.Width / pAlignment->CBufferAlignment + 1) * pAlignment->CBufferAlignment;
			}
			
			properties.Type = D3D12_HEAP_TYPE_UPLOAD;
			resState = D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_GENERIC_READ;
			GpuState = EGpuResourceState::GRS_GenericRead;

			auto allocInfo = device->mDevice->GetResourceAllocationInfo(0, 1, &resDesc);
			//allocator = device->mCBufferMemAllocator;
			mGpuMemory = device->mCBufferMemAllocator->Alloc(device, desc.Size);
		}
		if (desc.Type & EBufferType::BFT_RTV)
		{
			ASSERT(false);
			resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
		}
		if (desc.Type & EBufferType::BFT_DSV)
		{
			ASSERT(false);
			resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
		}
		if (desc.Type & EBufferType::BFT_UAV)
		{
			properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY::D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
			resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
			//mGpuMemory = device->mUavBufferMemAllocator->Alloc(device, desc.Size);
		}
		if (desc.Type & EBufferType::BFT_Vertex)
		{
			if (properties.Type == D3D12_HEAP_TYPE_DEFAULT)
			{
				//mGpuMemory = device->mDefaultBufferMemAllocator->Alloc(device, desc.Size);
			}
			else if (properties.Type == D3D12_HEAP_TYPE_UPLOAD)
			{
				//mGpuMemory = device->mUploadBufferMemAllocator->Alloc(device, desc.Size);
			}
		}
		if (desc.Type & EBufferType::BFT_Index)
		{
			if (properties.Type == D3D12_HEAP_TYPE_DEFAULT)
			{
				//mGpuMemory = device->mDefaultBufferMemAllocator->Alloc(device, desc.Size);
			}
			else if (properties.Type == D3D12_HEAP_TYPE_UPLOAD)
			{
				//mGpuMemory = device->mUploadBufferMemAllocator->Alloc(device, desc.Size);
			}
		}
		if (desc.Type & EBufferType::BFT_IndirectArgs)
		{
			resState |= D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT;
			GpuState = EGpuResourceState::GRS_UavIndirect;
		}
		if (desc.CpuAccess == ECpuAccess::CAS_READ)
		{
			resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			resState = D3D12_RESOURCE_STATE_COPY_DEST;
			GpuState = EGpuResourceState::GRS_CopyDst;
			//properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY::D3D12_CPU_PAGE_PROPERTY_WRITE_BACK;
		}
		if (mGpuMemory == nullptr)
		{
			mGpuMemory = DX12GpuDefaultMemAllocator::Alloc(device, &resDesc, &properties, resState);
		}

		/*if (allocator != nullptr)
		{
			mGpuMemory = allocator->Alloc(device, resDesc.Width);
			auto hr = device->mDevice->CreatePlacedResource(mGpuMemory->GpuHeap.UnsafeConvertTo<DX12GpuHeap>()->mDxHeap, mGpuMemory->Offset, &resDesc, resState, nullptr, IID_PPV_ARGS(mGpuResource.GetAddressOf()));
			if (FAILED(hr))
				return false;
		}
		else
		{
			//D3D12_CLEAR_VALUE data{};
			auto hr = device->mDevice->CreateCommittedResource(&properties, D3D12_HEAP_FLAG_NONE, &resDesc, resState, nullptr, IID_PPV_ARGS(mGpuResource.GetAddressOf()));
			if (FAILED(hr))
				return false;
		}*/
		//mGpuResource->SetName(L"Buffer");
		
		if (desc.InitData != nullptr)
		{
			if (properties.Type == D3D12_HEAP_TYPE_UPLOAD)
			{
				//auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);
				DX12CommandList* cmd = nullptr;
				FMappedSubResource mapped{};
				auto hr = this->Map(cmd, 0, &mapped, false);
				if (hr == S_OK)
				{
					BYTE* pCopyTar = (BYTE*)mapped.pData;
					BYTE* pCopySrc = (BYTE*)desc.InitData;
					memcpy(pCopyTar, pCopySrc, resDesc.Width);
					this->Unmap(cmd, 0);
				}
				//cmd->EndCommand();
				//device->GetCmdQueue()->ExecuteCommandList(cmd);
			}
			else
			{
				D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint{};
				UINT numX = 0;
				UINT64 rowSize = 0, totalSize = 0;
				device->mDevice->GetCopyableFootprints(&resDesc, 0, 1, 0, &footPrint, &numX, &rowSize, &totalSize);

				auto bf = CreateUploadBuffer(device, desc.InitData, totalSize, Desc.Size);

				auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);
				cmd->BeginCommand();
				auto saved = this->GpuState;
				this->TransitionTo(cmd, EGpuResourceState::GRS_CopyDst);
				//cmd->mContext->CopyBufferRegion(mGpuResource, footPrint.Offset, bf, 0, footPrint.Footprint.Width);
				auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
				cmd->mContext->CopyBufferRegion(pTarGpuResource, mGpuMemory->Offset, bf, 0, Desc.Size);
				this->TransitionTo(cmd, saved);
				cmd->EndCommand();

				device->GetCmdQueue()->ExecuteCommandList(cmd);
				device->GetCmdQueue()->ReleaseIdleCmdlist(cmd, EQueueCmdlist::QCL_Transient);

				device->DelayDestroy(bf);
			}
		}
		return true;
	}

	void DX12Buffer::TransitionTo(ICommandList* cmd, EGpuResourceState state)
	{
		if (Desc.Usage == EGpuUsage::USAGE_DYNAMIC)
		{
			ASSERT(GpuState == EGpuResourceState::GRS_GenericRead);
			return;
		}
		else if (Desc.Usage == EGpuUsage::USAGE_STAGING)
		{
			ASSERT(GpuState == EGpuResourceState::GRS_CopyDst || GpuState == EGpuResourceState::GRS_CopySrc);
			return;
		}
		
		if (state != 0)
		{
			if ((state & GpuState) == state)
				return;
		}
		else
		{
			if (state == GpuState)
				return;
		}

		cmd->SetBufferBarrier(this, EPipelineStage::PPLS_ALL_COMMANDS, EPipelineStage::PPLS_ALL_COMMANDS, GpuState, state);

		/*D3D12_RESOURCE_BARRIER tmp{};
		tmp.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
		tmp.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
		auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
		tmp.Transition.pResource = pTarGpuResource;
		tmp.Transition.StateBefore = GpuStateToDX12(GpuState);
		tmp.Transition.StateAfter = GpuStateToDX12(state);
		tmp.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
		((DX12CommandList*)cmd)->mContext->ResourceBarrier(1, &tmp);*/
		GpuState = state;
	}

	void DX12Buffer::Flush2Device(ICommandList* cmd, void* pBuffer, UINT Size)
	{
		if (Size > Desc.Size || pBuffer == NULL)
			return;
		ASSERT(Size == Desc.Size);

		FMappedSubResource mapped{};
		if (this->Map(cmd, 0, &mapped, false))
		{
			memcpy(mapped.pData, pBuffer, Size);
			this->Unmap(cmd, 0);
		}
	}

	void DX12Buffer::UpdateGpuData(ICommandList* cmd, UINT subRes, void* pData, const FSubresourceBox* box, UINT rowPitch, UINT depthPitch)
	{
		if (Desc.Usage == EGpuUsage::USAGE_DEFAULT)
		{
			auto device = mDeviceRef.GetPtr();
			D3D12_RESOURCE_DESC resDesc{};
			resDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
			//resDesc.Width = desc.Size;
			resDesc.Width = Desc.Size;
			resDesc.Height = 1;
			resDesc.DepthOrArraySize = 1;
			resDesc.MipLevels = 1;
			resDesc.SampleDesc.Count = 1;
			resDesc.SampleDesc.Quality = 0;
			resDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
			resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			resDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

			auto pAlignment = device->GetGpuResourceAlignment();
			if (Desc.Type & EBufferType::BFT_CBuffer)
			{
				//resDesc.Alignment = pAlignment->CBufferAlignment;
				if (resDesc.Width % pAlignment->CBufferAlignment)
				{
					resDesc.Width = (resDesc.Width / pAlignment->CBufferAlignment + 1) * pAlignment->CBufferAlignment;
				}
			}
			if (Desc.Type & EBufferType::BFT_RTV)
			{
				ASSERT(false);
				resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
			}
			if (Desc.Type & EBufferType::BFT_DSV)
			{
				ASSERT(false);
				resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
			}
			if (Desc.Type & EBufferType::BFT_UAV)
			{
				resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
			}

			D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint{};
			UINT numX = 0;
			UINT64 rowSize = 0, totalSize = 0;
			device->mDevice->GetCopyableFootprints(&resDesc, 0, 1, 0, &footPrint, &numX, &rowSize, &totalSize);

			auto bf = CreateUploadBuffer(device, pData, totalSize, rowPitch);//.Size);

			auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);
			cmd->BeginCommand();
			auto saved = this->GpuState;
			this->TransitionTo(cmd, EGpuResourceState::GRS_CopyDst);
			auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
			cmd->mContext->CopyBufferRegion(pTarGpuResource, mGpuMemory->Offset, bf, 0, rowPitch);
			this->TransitionTo(cmd, saved);
			cmd->EndCommand();

			/*auto fence = cmd->mCommitFence;
			auto tarValue = fence->GetCompletedValue() + 1;*/
			device->GetCmdQueue()->ExecuteCommandList(cmd);
			device->GetCmdQueue()->ReleaseIdleCmdlist(cmd, EQueueCmdlist::QCL_Transient);

			device->DelayDestroy(bf);
			/*device->PushPostEvent([fence, bf, tarValue](IGpuDevice* pDevice, UINT64 frameCount)->bool
				{
					if (fence->GetCompletedValue() >= tarValue)
					{
						pDevice->DelayDestroy(bf);
						return true;
					}
					return false;
				});*/
		}
		else
		{
			FMappedSubResource mapped{};
			if (this->Map(cmd, subRes, &mapped, false))
			{
				memcpy(mapped.pData, pData, rowPitch);
				this->Unmap(cmd, subRes);
			}
		}
	}

	void DX12Buffer::UpdateGpuData(ICommandList* cmd, UINT offset, void* pData, UINT size)
	{
		if (Desc.Usage == EGpuUsage::USAGE_DEFAULT)
		{
			auto device = mDeviceRef.GetPtr();
			D3D12_RESOURCE_DESC resDesc{};
			resDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
			//resDesc.Width = desc.Size;
			resDesc.Width = Desc.Size;
			resDesc.Height = 1;
			resDesc.DepthOrArraySize = 1;
			resDesc.MipLevels = 1;
			resDesc.SampleDesc.Count = 1;
			resDesc.SampleDesc.Quality = 0;
			resDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
			resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			resDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

			auto pAlignment = device->GetGpuResourceAlignment();
			if (Desc.Type & EBufferType::BFT_CBuffer)
			{
				//resDesc.Alignment = pAlignment->CBufferAlignment;
				if (resDesc.Width % pAlignment->CBufferAlignment)
				{
					resDesc.Width = (resDesc.Width / pAlignment->CBufferAlignment + 1) * pAlignment->CBufferAlignment;
				}
			}
			if (Desc.Type & EBufferType::BFT_RTV)
			{
				ASSERT(false);
				resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
			}
			if (Desc.Type & EBufferType::BFT_DSV)
			{
				ASSERT(false);
				resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
			}
			if (Desc.Type & EBufferType::BFT_UAV)
			{
				resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
			}

			D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint{};
			UINT numX = 0;
			UINT64 rowSize = 0, totalSize = 0;
			device->mDevice->GetCopyableFootprints(&resDesc, 0, 1, 0, &footPrint, &numX, &rowSize, &totalSize);

			auto bf = CreateUploadBuffer(device, pData, totalSize, size);//.Size);

			auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);			
			cmd->BeginCommand();
			auto saved = this->GpuState;
			this->TransitionTo(cmd, EGpuResourceState::GRS_CopyDst);
			auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
			cmd->mContext->CopyBufferRegion(pTarGpuResource, mGpuMemory->Offset, bf, 0, size);
			this->TransitionTo(cmd, saved);
			cmd->EndCommand();

			/*auto fence = cmd->mCommitFence;
			auto tarValue = fence->GetCompletedValue() + 1;*/
			device->GetCmdQueue()->ExecuteCommandList(cmd);
			device->GetCmdQueue()->ReleaseIdleCmdlist(cmd, EQueueCmdlist::QCL_Transient);

			device->DelayDestroy(bf);
			/*device->PushPostEvent([fence, bf, tarValue](IGpuDevice* pDevice)->bool
				{
					if (fence->GetCompletedValue() >= tarValue)
					{
						pDevice->DelayDestroy(bf);
						return true;
					}
					return false;
				});*/
		}
		else
		{
			FMappedSubResource mapped{};
			if (this->Map(cmd, 0, &mapped, false))
			{
				memcpy(mapped.pData, pData, size);
				this->Unmap(cmd, 0);
			}
		}
	}

	bool DX12Buffer::Map(ICommandList* cmd, UINT index, FMappedSubResource* res, bool forRead)
	{
		if (Desc.Usage == EGpuUsage::USAGE_DEFAULT)
		{
			ASSERT(false);
			return false;
		}
		else
		{
			D3D12_RANGE range{};
			range.Begin = mGpuMemory->Offset;
			range.End = range.Begin + Desc.Size;
			auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
			void* pData = nullptr;
			if (pTarGpuResource->Map(0, &range, &pData) != S_OK)
				return false;
			res->pData = ((BYTE*)pData) + range.Begin;
			res->RowPitch = Desc.RowPitch;
			res->DepthPitch = Desc.DepthPitch;
		}
		
		return true;
	}

	void DX12Buffer::Unmap(ICommandList* cmd, UINT index)
	{
		D3D12_RANGE range{};
		range.Begin = mGpuMemory->Offset;
		range.End = range.Begin + Desc.Size;

		auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
		pTarGpuResource->Unmap(index, nullptr);
	}

	void DX12Buffer::SetDebugName(const char* name)
	{
		auto pTarGpuResource = ((DX12GpuHeap*)mGpuMemory->GpuHeap)->mGpuResource;
		std::wstring n = StringHelper::strtowstr(name);		
		pTarGpuResource->SetName(n.c_str());
	}
	
	DX12Texture::DX12Texture()
	{
	}

	DX12Texture::~DX12Texture()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;
		device->DelayDestroy(mGpuResource);
		mGpuResource = nullptr;
	}
	D3D12_RESOURCE_FLAGS BufferTypeToDXBindFlags(EBufferType type)
	{
		D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAG_NONE;		
		if (type & EBufferType::BFT_UAV)
		{
			flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
		}
		if (type & EBufferType::BFT_RTV)
		{
			flags |= D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
		}
		if (type & EBufferType::BFT_DSV)
		{
			flags |= D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
		}
		return flags;
	}
	AutoRef<ID3D12Resource> CreateUploadResource(DX12GpuDevice* device, UINT rowPitch, UINT64 uploadSize, UINT64 rowSize, UINT numOfRows, EPixelFormat format, FMappedSubResource* mappedResource)
	{
		D3D12_HEAP_PROPERTIES properties{};
		properties.Type = D3D12_HEAP_TYPE_UPLOAD;
		properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		properties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
		D3D12_RESOURCE_DESC resDesc{};
		resDesc.SampleDesc.Count = 1;
		resDesc.SampleDesc.Quality = 0;
		resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
		resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
		resDesc.MipLevels = 1;
		resDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
		resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
		resDesc.Alignment = 0;
		resDesc.Width = uploadSize;
		resDesc.Height = 1;
		resDesc.DepthOrArraySize = 1;
		resDesc.MipLevels = 1;
		resDesc.Format = DXGI_FORMAT_UNKNOWN;
		resDesc.SampleDesc.Count = 1;
		resDesc.SampleDesc.Quality = 0;
		resDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
		resDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

		AutoRef<ID3D12Resource> uploadBuffer;
		auto hr = device->mDevice->CreateCommittedResource(&properties, D3D12_HEAP_FLAG_NONE, &resDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ, NULL, IID_PPV_ARGS(uploadBuffer.GetAddressOf()));
		ASSERT(hr == S_OK);
		BYTE* mapped = NULL;
		D3D12_RANGE range = { 0, uploadSize };
		hr = uploadBuffer->Map(0, nullptr, (void**)&mapped);
		if (hr == S_OK)
		{
			BYTE* pCopyTar = mapped;
			BYTE* pCopySrc = (BYTE*)mappedResource->pData;
			for (UINT y = 0; y < numOfRows; y++)
			{
				memcpy(pCopyTar, pCopySrc, rowSize);
				pCopyTar += rowPitch;
				pCopySrc += mappedResource->RowPitch;
			}
			uploadBuffer->Unmap(0, &range);
		}

		return uploadBuffer;
	}
	bool DX12Texture::Init(DX12GpuDevice* device, const FTextureDesc& desc)
	{
		Desc = desc;
		mDeviceRef.FromObject(device);
		
		D3D12_RESOURCE_STATES resState = D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_COMMON;
		GpuState = EGpuResourceState::GRS_Undefine;
		D3D12_HEAP_PROPERTIES properties{};
		properties.Type = D3D12_HEAP_TYPE_DEFAULT;
		D3D12_RESOURCE_DESC resDesc{};
		resDesc.SampleDesc.Count = desc.SamplerDesc.Count;
		resDesc.SampleDesc.Quality = desc.SamplerDesc.Quality;
		resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
		resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
		resDesc.MipLevels = desc.MipLevels;
		resDesc.Format = FormatToDX12Format(desc.Format);
		resDesc.Width = desc.Width;
		resDesc.Height = desc.Height;
		/*if (desc.Depth > 0)
		{
			resDesc.DepthOrArraySize = desc.Depth;
			resDesc.Alignment = device->GetGpuResourceAlignment()->TextureAlignment;
		}
		else
		{
			resDesc.DepthOrArraySize = desc.ArraySize;
			resDesc.Alignment = device->GetGpuResourceAlignment()->TexturePitchAlignment;
		}*/
		resDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

		resDesc.Flags = BufferTypeToDXBindFlags(desc.BindFlags);

		if (Desc.CpuAccess & ECpuAccess::CAS_READ)
		{
			ASSERT(Desc.Usage == EGpuUsage::USAGE_STAGING);
			properties.Type = D3D12_HEAP_TYPE_READBACK;
		}

		switch (GetDimension())
		{
			case 1:
			{
				resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE1D;
				resDesc.Width = desc.Width;
				resDesc.Height = 1;
				resDesc.DepthOrArraySize = desc.ArraySize;
				break;
			case 2:
			{
				resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
				resDesc.Width = desc.Width;
				resDesc.Height = desc.Height;
				resDesc.DepthOrArraySize = desc.ArraySize;
			}
			break;
			case 3:
			{
				resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
				resDesc.Width = desc.Width;
				resDesc.Height = desc.Height;
				resDesc.DepthOrArraySize = desc.Depth;
			}
			break;
			default:
				return false;
			}
		}
		D3D12_CLEAR_VALUE data{};
		data.Format = FormatToDX12Format(desc.Format);
		data.Color[0] = 0;
		data.Color[1] = 0;
		data.Color[2] = 0;
		data.Color[3] = 0;
		data.DepthStencil.Depth = 1.0f;
		data.DepthStencil.Stencil = 0;

		/*if (desc.InitData != nullptr)
		{
			resState |= D3D12_RESOURCE_STATES::D3D12_RESOURCE_STATE_COPY_DEST;
		}*/
		if (desc.BindFlags & EBufferType::BFT_DSV)
		{
			/*switch (desc.Format)
			{
				case EPixelFormat::PXF_D24_UNORM_S8_UINT:
					data.Format = DXGI_FORMAT_R24G8_TYPELESS;
					break;
				case EPixelFormat::PXF_D32_FLOAT:
					data.Format = DXGI_FORMAT_R32_TYPELESS;
					break;
				case EPixelFormat::PXF_D16_UNORM:
					data.Format = DXGI_FORMAT_R16_TYPELESS;
					break;
				case EPixelFormat::PXF_UNKNOWN:
					data.Format = DXGI_FORMAT_R16_TYPELESS;
					break;
				default:
					break;
			}
			if (data.Format == DXGI_FORMAT::DXGI_FORMAT_R24G8_TYPELESS)
			{
				data.Format = DXGI_FORMAT::DXGI_FORMAT_D24_UNORM_S8_UINT;
			}
			else if (data.Format == DXGI_FORMAT::DXGI_FORMAT_R32_TYPELESS)
			{
				data.Format = DXGI_FORMAT::DXGI_FORMAT_D32_FLOAT;
			}
			else if (data.Format == DXGI_FORMAT::DXGI_FORMAT_R16_TYPELESS)
			{
				data.Format = DXGI_FORMAT::DXGI_FORMAT_D16_UNORM;
			}*/
			auto hr = device->mDevice->CreateCommittedResource(&properties, D3D12_HEAP_FLAG_NONE, &resDesc, resState, &data, IID_PPV_ARGS(mGpuResource.GetAddressOf()));
			if (FAILED(hr))
				return false;
		}
		else
		{
			auto hr = device->mDevice->CreateCommittedResource(&properties, D3D12_HEAP_FLAG_NONE, &resDesc, resState, nullptr, IID_PPV_ARGS(mGpuResource.GetAddressOf()));
			if (FAILED(hr))
				return false;
		}
		//mGpuResource->SetName(L"Texture");
		
		if (desc.InitData != nullptr)
		{
			UINT w = Desc.Width;
			UINT h = Desc.Height;
			std::vector<AutoRef<ID3D12Resource>> mipBuffers;
			std::vector<D3D12_TEXTURE_COPY_LOCATION> srcLocations;
			//for (UINT i = 0; i < desc.ArraySize; i++)

			//((FTextureDesc*)&desc)->MipLevels = 1;
			for (UINT j = 0; j < desc.MipLevels; j++)
			{
				D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint{};
				UINT numX;
				UINT64 rowSize, totalSize;
				device->mDevice->GetCopyableFootprints(&resDesc, j, 1, 0, &footPrint, &numX, &rowSize, &totalSize);

				auto bf = CreateUploadResource(device, footPrint.Footprint.RowPitch, totalSize, rowSize, numX, Desc.Format, &desc.InitData[j]);
				mipBuffers.push_back(bf);

				D3D12_TEXTURE_COPY_LOCATION srcLocation = {};
				srcLocation.pResource = bf;
				srcLocation.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
				srcLocation.PlacedFootprint = footPrint;
				srcLocation.PlacedFootprint.Footprint.Format = FormatToDX12Format(this->Desc.Format);
				
				srcLocations.push_back(srcLocation);

				w = w / 2;
				h = h / 2;
				if (w == 0)
					w = 1;
				if (h == 0)
					h = 1;
			}


			D3D12_TEXTURE_COPY_LOCATION dstLocation = {};
			dstLocation.pResource = this->mGpuResource;
			dstLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
			dstLocation.SubresourceIndex = 0;

			auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);
			cmd->BeginCommand();
			//auto saved = this->GpuState;
			this->TransitionTo(cmd, EGpuResourceState::GRS_CopyDst);
			for (UINT j = 0; j < desc.MipLevels; j++)
			{
				dstLocation.SubresourceIndex = j;
				cmd->mContext->CopyTextureRegion(&dstLocation, 0, 0, 0, &srcLocations[j], NULL);
			}
			this->TransitionTo(cmd, EGpuResourceState::GRS_Undefine);
			cmd->EndCommand();

			device->GetCmdQueue()->ExecuteCommandList(cmd);
			device->GetCmdQueue()->ReleaseIdleCmdlist(cmd, EQueueCmdlist::QCL_Transient);

			device->DelayDestroy(mipBuffers);
		}
		else
		{
			auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);
			cmd->BeginCommand();
			this->TransitionTo(cmd, EGpuResourceState::GRS_GenericRead);
			cmd->EndCommand();
			device->GetCmdQueue()->ExecuteCommandList(cmd);
			device->GetCmdQueue()->ReleaseIdleCmdlist(cmd, EQueueCmdlist::QCL_Transient);
		}
			
		return true;
	}

	IGpuBufferData* DX12Texture::CreateBufferData(IGpuDevice* device, UINT mipIndex, ECpuAccess cpuAccess, FSubResourceFootPrint* outFootPrint)
	{
		FBufferDesc desc{};
		desc.CpuAccess = cpuAccess;
		desc.Type = EBufferType::BFT_SRV;
		if (cpuAccess & ECpuAccess::CAS_READ)
		{
			desc.Usage = EGpuUsage::USAGE_STAGING;
			desc.Type = EBufferType::BFT_NONE;
		}
		else if (cpuAccess & ECpuAccess::CAS_WRITE)
			desc.Usage = EGpuUsage::USAGE_DYNAMIC;
		else
			desc.Usage = EGpuUsage::USAGE_DEFAULT;
		
		desc.StructureStride = GetPixelByteWidth(Desc.Format);
		desc.RowPitch = desc.StructureStride * Desc.Width;
		auto pAlignment = device->GetGpuResourceAlignment();
		if (desc.RowPitch % pAlignment->TexturePitchAlignment > 0)
		{
			desc.RowPitch = (desc.RowPitch / pAlignment->TexturePitchAlignment + 1) * pAlignment->TexturePitchAlignment;
		}
		desc.DepthPitch = desc.RowPitch * Desc.Height;
		desc.Size = desc.DepthPitch;

		auto result = device->CreateBuffer(&desc);

		outFootPrint->X = 0;
		outFootPrint->Y = 0;
		outFootPrint->Z = 0;
		outFootPrint->Width = Desc.Width;
		outFootPrint->Height = Desc.Height;
		if (outFootPrint->Height == 0)
			outFootPrint->Height = 1;
		outFootPrint->Depth = Desc.Depth;
		if (outFootPrint->Depth == 0)
			outFootPrint->Depth = 1;
		outFootPrint->Format = Desc.Format;
		outFootPrint->RowPitch = desc.RowPitch;
		return result;
	}

	bool DX12Texture::Map(ICommandList* cmd, UINT subRes, FMappedSubResource* res, bool forRead)
	{
		UINT pitch = Desc.Width * GetPixelByteWidth(Desc.Format);//todo
		UINT pitchAlignment = cmd->GetGpuDevice()->GetGpuResourceAlignment()->TexturePitchAlignment;
		UINT texAlignment = cmd->GetGpuDevice()->GetGpuResourceAlignment()->TextureAlignment;
		if (pitch % pitchAlignment != 0)
		{
			pitch = (pitch / pitchAlignment + 1) * pitchAlignment;
		}
		UINT texResSize = pitch * Desc.Height;
		if (texResSize % texAlignment != 0)
		{
			pitch = (pitch / texAlignment + 1) * texAlignment;
		}
		D3D12_RANGE range{};
		if (forRead)
		{
			range.Begin = 0;
			range.End = texResSize;
		}
		//UINT subRes = mipIndex + Desc.MipLevels * arrayIndex;
		void* pTarData = nullptr;
		auto hr = mGpuResource->Map(subRes, &range, &pTarData);
		if (hr != S_OK)
		{
			return false;
		}
		res->pData = pTarData;
		res->RowPitch = pitch;
		res->DepthPitch = texResSize;
		return true;
	}
	void DX12Texture::Unmap(ICommandList* cmd, UINT subRes)
	{
		//UINT subRes = mipIndex + Desc.MipLevels * arrayIndex;
		mGpuResource->Unmap(subRes, nullptr);
	}

	void DX12Texture::UpdateGpuData(ICommandList* cmd, UINT subRes, void* pData, const FSubresourceBox* box, UINT rowPitch, UINT depthPitch)
	{
		//UINT subRes = mipIndex + Desc.MipLevels * arrayIndex;
		if (Desc.Usage == EGpuUsage::USAGE_DEFAULT)
		{
			auto device = mDeviceRef.GetPtr();
			D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint{};
			UINT numX;
			UINT64 rowSize, totalSize;
			D3D12_RESOURCE_DESC resDesc{};
			resDesc.SampleDesc.Count = Desc.SamplerDesc.Count;
			resDesc.SampleDesc.Quality = Desc.SamplerDesc.Quality;
			resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
			resDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			resDesc.MipLevels = Desc.MipLevels;
			resDesc.Format = FormatToDX12Format(Desc.Format);
			resDesc.Width = Desc.Width;
			resDesc.Height = Desc.Height;
			resDesc.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
			resDesc.Flags = BufferTypeToDXBindFlags(Desc.BindFlags);
			switch (GetDimension())
			{
				case 1:
				{
					resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE1D;
					resDesc.Width = Desc.Width;
					resDesc.Height = 1;
					resDesc.DepthOrArraySize = Desc.ArraySize;
				}
				break;
				case 2:
				{
					resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
					resDesc.Width = Desc.Width;
					resDesc.Height = Desc.Height;
					resDesc.DepthOrArraySize = Desc.ArraySize;
				}
				break;
				case 3:
				{
					resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
					resDesc.Width = Desc.Width;
					resDesc.Height = Desc.Height;
					resDesc.DepthOrArraySize = Desc.Depth;
				}
				break;
				default:
					break;
			}

			device->mDevice->GetCopyableFootprints(&resDesc, subRes, 1, 0, &footPrint, &numX, &rowSize, &totalSize);
			
			FMappedSubResource initData{};
			initData.pData = pData;
			initData.RowPitch = rowPitch;
			initData.DepthPitch = depthPitch;
			auto bf = CreateUploadResource(device, footPrint.Footprint.RowPitch, totalSize, rowSize, numX, Desc.Format, &initData);
			D3D12_TEXTURE_COPY_LOCATION srcLocation = {};
			srcLocation.pResource = bf;
			srcLocation.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
			srcLocation.PlacedFootprint = footPrint;
			srcLocation.PlacedFootprint.Footprint.Format = FormatToDX12Format(this->Desc.Format);

			D3D12_TEXTURE_COPY_LOCATION dstLocation = {};
			dstLocation.pResource = this->mGpuResource;
			dstLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
			dstLocation.SubresourceIndex = 0;

			auto cmd = (DX12CommandList*)device->GetCmdQueue()->GetIdleCmdlist(EQueueCmdlist::QCL_Transient);
			cmd->BeginCommand();
			auto saved = this->GpuState;
			this->TransitionTo(cmd, EGpuResourceState::GRS_CopyDst);
			cmd->mContext->CopyTextureRegion(&dstLocation, 0, 0, 0, &srcLocation, NULL);
			this->TransitionTo(cmd, saved);
			cmd->EndCommand();
			
			/*auto fence = cmd->mCommitFence;
			auto tarValue = fence->GetCompletedValue() + 1;*/
			device->GetCmdQueue()->ExecuteCommandList(cmd);
			device->GetCmdQueue()->ReleaseIdleCmdlist(cmd, EQueueCmdlist::QCL_Transient);

			device->DelayDestroy(bf);
			/*device->PushPostEvent([fence, bf, tarValue](IGpuDevice* pDevice)->bool
				{
					if (fence->GetCompletedValue() >= tarValue)
					{
						pDevice->DelayDestroy(bf);
						return true;
					}
					return false;
				});*/
		}
		else //if (Desc.Usage == EGpuUsage::USAGE_DYNAMIC || Desc.Usage == EGpuUsage::USAGE_STAGING)
		{
			auto refCmdList = (DX12CommandList*)cmd;
			//refCmdList->mContext->UpdateSubresource(mTexture1D, subRes, (D3D11_BOX*)box, pData, rowPitch, depthPitch);
			D3D12_RANGE range{};
			void* pTarData = nullptr;
			if (mGpuResource->Map(0, &range, &pTarData) == S_OK)
			{
				memcpy(pTarData, pData, rowPitch);
				mGpuResource->Unmap(0, nullptr);
			}
		}
	}

	void DX12Texture::TransitionTo(ICommandList* cmd, EGpuResourceState state)
	{
		if (state != 0)
		{
			if ((state & GpuState) == state)
				return;
		}
		else
		{
			if (state == GpuState)
				return;
		}
		cmd->SetTextureBarrier(this, EPipelineStage::PPLS_ALL_COMMANDS, EPipelineStage::PPLS_ALL_COMMANDS, GpuState, state);

		/*D3D12_RESOURCE_BARRIER tmp{};
		tmp.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
		tmp.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
		tmp.Transition.pResource = mGpuResource;
		tmp.Transition.StateBefore = GpuStateToDX12(GpuState);
		tmp.Transition.StateAfter = GpuStateToDX12(state);
		if (tmp.Transition.StateBefore == tmp.Transition.StateAfter)
			return;
		tmp.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
		((DX12CommandList*)cmd)->mContext->ResourceBarrier(1, &tmp);*/

		GpuState = state;
	}

	void DX12Texture::SetDebugName(const char* name)
	{
		std::wstring n = StringHelper::strtowstr(name);
		mGpuResource->SetName(n.c_str());
	}

	DX12CbView::DX12CbView()
	{

	}
	DX12CbView::~DX12CbView()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;
		device->DelayDestroy(mView); 
		mView = nullptr;
	}
	bool DX12CbView::Init(DX12GpuDevice* device, IBuffer* pBuffer, const FCbvDesc& desc)
	{
		mDeviceRef.FromObject(device);
		ShaderBinder = desc.ShaderBinder;

		UINT alignedSize = 1;
		if(ShaderBinder!=nullptr)
			alignedSize = ShaderBinder->Size;
		auto pAlignment = device->GetGpuResourceAlignment();
		if (alignedSize % pAlignment->CBufferAlignment)
		{
			alignedSize = (alignedSize / pAlignment->CBufferAlignment + 1) * pAlignment->CBufferAlignment;
		}
		//alignedSize = (alignedSize + 255) & (~255);

		if (pBuffer == nullptr)
		{
			FBufferDesc bfDesc{};
			bfDesc.SetDefault();
			bfDesc.Size = alignedSize;
			bfDesc.StructureStride = 0;
			bfDesc.InitData = nullptr;
			bfDesc.Type = EBufferType::BFT_CBuffer;
			bfDesc.Usage = EGpuUsage::USAGE_DYNAMIC;
			bfDesc.CpuAccess = ECpuAccess::CAS_WRITE;

			Buffer = MakeWeakRef(device->CreateBuffer(&bfDesc));
			ASSERT(Buffer != nullptr);
		}
		else
		{
			Buffer = pBuffer;
		}
		
		mView = device->mSrvAllocHeapManager->Alloc<DX12DescriptorSetPagedObject>();

		D3D12_CONSTANT_BUFFER_VIEW_DESC d3dDesc{};
		d3dDesc.BufferLocation = Buffer.UnsafeConvertTo<DX12Buffer>()->GetGPUVirtualAddress();
		d3dDesc.SizeInBytes = alignedSize;// Buffer->Desc.Size;

		device->mDevice->CreateConstantBufferView(&d3dDesc, mView->GetHandle(0));

		return true;
	}
	bool DX12VbView::Init(DX12GpuDevice* device, IBuffer* pBuffer, const FVbvDesc* desc)
	{
		Desc = *desc;
		Desc.InitData = nullptr;
		if (pBuffer == nullptr)
		{
			FBufferDesc bfDesc{};
			bfDesc.SetDefault();
			bfDesc.Size = desc->Size;
			bfDesc.StructureStride = desc->Stride;
			bfDesc.InitData = desc->InitData;
			bfDesc.Type = EBufferType::BFT_Vertex;
			bfDesc.Usage = desc->Usage;
			bfDesc.CpuAccess = desc->CpuAccess;
			Buffer = MakeWeakRef(device->CreateBuffer(&bfDesc));
			ASSERT(Buffer != nullptr);
		}
		else
		{
			Buffer = pBuffer;
		}
		return true;
	}

	bool DX12IbView::Init(DX12GpuDevice* device, IBuffer* pBuffer, const FIbvDesc* desc)
	{
		Desc = *desc;
		Desc.InitData = nullptr;
		if (pBuffer == nullptr)
		{
			FBufferDesc bfDesc{};
			bfDesc.SetDefault();
			bfDesc.Size = desc->Size;
			bfDesc.StructureStride = desc->Stride;
			bfDesc.InitData = desc->InitData;
			bfDesc.Type = EBufferType::BFT_Index;
			bfDesc.Usage = desc->Usage;
			bfDesc.CpuAccess = desc->CpuAccess;
			Buffer = MakeWeakRef(device->CreateBuffer(&bfDesc));
			ASSERT(Buffer != nullptr);
		}
		else
		{
			Buffer = pBuffer;
		}
		return true;
	}

	DX12SrView::DX12SrView()
	{
	}

	DX12SrView::~DX12SrView()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;
		device->DelayDestroy(mView);
		mView = nullptr;
	}

	void SrvDesc2DX(D3D12_SHADER_RESOURCE_VIEW_DESC* tar, const FSrvDesc* src)
	{
		//memset(tar, 0, sizeof(D3D12_SHADER_RESOURCE_VIEW_DESC));
		tar->Format = FormatToDX12Format(src->Format);
		switch (src->Type)
		{
			case ST_BufferSRV:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_BUFFER;
				tar->Buffer.FirstElement = src->Buffer.FirstElement;
				tar->Buffer.NumElements = src->Buffer.NumElements;
				tar->Buffer.StructureByteStride = src->Buffer.StructureByteStride;
				tar->Buffer.Flags = (D3D12_BUFFER_SRV_FLAGS)src->Buffer.Flags;
			}
			break;
			case ST_Texture1D:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE1D;
				tar->Texture1D.MipLevels = src->Texture1D.MipLevels;
				tar->Texture1D.MostDetailedMip = src->Texture1D.MostDetailedMip;
			}
			break;
			case ST_Texture1DArray:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE1DARRAY;
				tar->Texture1DArray.MipLevels = src->Texture1DArray.MipLevels;
				tar->Texture1DArray.MostDetailedMip = src->Texture1DArray.MostDetailedMip;
				tar->Texture1DArray.ArraySize = src->Texture1DArray.ArraySize;
				tar->Texture1DArray.FirstArraySlice = src->Texture1DArray.FirstArraySlice;
			}
			break;
			case ST_Texture2D:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE2D;
				tar->Texture2D.MipLevels = src->Texture2D.MipLevels;
				tar->Texture2D.MostDetailedMip = src->Texture2D.MostDetailedMip;
				tar->Texture2D.ResourceMinLODClamp = src->Texture2D.ResourceMinLODClamp;
				tar->Texture2D.PlaneSlice = src->Texture2D.PlaneSlice;
			}
			break;
			case ST_Texture2DArray:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE2DARRAY;
				tar->Texture2DArray.ArraySize = src->Texture2DArray.ArraySize;
				tar->Texture2DArray.FirstArraySlice = src->Texture2DArray.FirstArraySlice;
				tar->Texture2DArray.MipLevels = src->Texture2DArray.MipLevels;
				tar->Texture2DArray.MostDetailedMip = src->Texture2DArray.MostDetailedMip;
			}
			break;
			case ST_Texture2DMS:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE2DMS;
				ASSERT(false);
			}
			break;
			case ST_Texture2DMSArray:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE2DMSARRAY;
				tar->Texture2DMSArray.ArraySize = src->Texture2DMSArray.ArraySize;
				tar->Texture2DMSArray.FirstArraySlice = src->Texture2DMSArray.FirstArraySlice;
				ASSERT(false);
			}
			break;
			case ST_Texture3D:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURE3D;
				tar->Texture3D.MipLevels = src->Texture3D.MipLevels;
				tar->Texture3D.MostDetailedMip = src->Texture3D.MostDetailedMip;
				ASSERT(false);
			}
			break;
			case ST_TextureCube:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURECUBE;
				tar->TextureCube.MipLevels = src->TextureCube.MipLevels;
				tar->TextureCube.MostDetailedMip = src->TextureCube.MostDetailedMip;
				ASSERT(false);
			}
			break;
			case ST_TextureCubeArray:
			{
				tar->ViewDimension = D3D12_SRV_DIMENSION::D3D12_SRV_DIMENSION_TEXTURECUBEARRAY;
				tar->TextureCubeArray.First2DArrayFace = src->TextureCubeArray.First2DArrayFace;
				tar->TextureCubeArray.MipLevels = src->TextureCubeArray.MipLevels;
				tar->TextureCubeArray.MostDetailedMip = src->TextureCubeArray.MostDetailedMip;
				tar->TextureCubeArray.NumCubes = src->TextureCubeArray.NumCubes;
				ASSERT(false);
			}
			break;
			default:
				ASSERT(false);
				break;
		}

		switch (tar->Format)
		{
			case DXGI_FORMAT_D24_UNORM_S8_UINT:
			case DXGI_FORMAT_R24G8_TYPELESS:
				tar->Format = DXGI_FORMAT_R24_UNORM_X8_TYPELESS;
				break;
			case DXGI_FORMAT_D32_FLOAT:
			case DXGI_FORMAT_R32_TYPELESS:
				{
					if (src->Type != ESrvType::ST_BufferSRV)
					{
						tar->Format = DXGI_FORMAT_R32_FLOAT;
					}
				}
				break;
			case DXGI_FORMAT_D16_UNORM:
			case DXGI_FORMAT_R16_TYPELESS:
				tar->Format = DXGI_FORMAT_R16_UNORM;
				break;
		}
	}

	bool DX12SrView::Init(DX12GpuDevice* device, IGpuBufferData* pBuffer, const FSrvDesc& desc)
	{
		mDeviceRef.FromObject(device);
		Desc = desc;
		Buffer = pBuffer;
		mView = device->mSrvAllocHeapManager->Alloc<DX12DescriptorSetPagedObject>();
		//
		D3D12_SHADER_RESOURCE_VIEW_DESC d3dDesc{};
		SrvDesc2DX(&d3dDesc, &desc);
		d3dDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		
		if (desc.Type == ST_BufferSRV)
		{
			d3dDesc.Format = DXGI_FORMAT::DXGI_FORMAT_UNKNOWN;
			//d3dDesc.Format = DXGI_FORMAT::DXGI_FORMAT_R32_TYPELESS;
		}
		if (d3dDesc.Buffer.Flags & D3D12_BUFFER_UAV_FLAGS::D3D12_BUFFER_UAV_FLAG_RAW)
		{
			d3dDesc.Format = DXGI_FORMAT::DXGI_FORMAT_R32_TYPELESS;
			d3dDesc.Buffer.StructureByteStride = 0;
		}
		auto pD3DRes = (ID3D12Resource*)pBuffer->GetHWBuffer();
		device->mDevice->CreateShaderResourceView(pD3DRes, &d3dDesc, mView->GetHandle(0));
		
		return true;
	}
	bool DX12SrView::UpdateBuffer(IGpuDevice* device, IGpuBufferData* pBuffer)
	{
		Buffer = pBuffer;
		mFingerPrint++;
		D3D12_SHADER_RESOURCE_VIEW_DESC d3dDesc{};
		d3dDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		if (Desc.Type == ESrvType::ST_Texture1D ||
			Desc.Type == ESrvType::ST_Texture2D ||
			Desc.Type == ESrvType::ST_Texture3D)
		{
			Desc.Texture2D.MipLevels = Buffer.UnsafeConvertTo<ITexture>()->Desc.MipLevels;
		}
		SrvDesc2DX(&d3dDesc, &Desc);

		auto pD3DRes = (ID3D12Resource*)pBuffer->GetHWBuffer();
		((DX12GpuDevice*)device)->mDevice->CreateShaderResourceView(pD3DRes, &d3dDesc, mView->GetHandle(0));
		return true;
	}

	DX12UaView::DX12UaView()
	{
		mView = nullptr;
	}

	DX12UaView::~DX12UaView()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;
		device->DelayDestroy(mView);
	}

	static void UavDesc2DX(IGpuResource* pBuffer, D3D12_UNORDERED_ACCESS_VIEW_DESC* tar, const FUavDesc* src)
	{
		//memset(tar, 0, sizeof(D3D12_SHADER_RESOURCE_VIEW_DESC));
		tar->Format = FormatToDX12Format(src->Format);
		switch (src->ViewDimension)
		{
			case EDimensionUAV::UAV_DIMENSION_BUFFER:
			{
				tar->ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_BUFFER;
				tar->Buffer.FirstElement = src->Buffer.FirstElement;
				tar->Buffer.NumElements = src->Buffer.NumElements;
				tar->Buffer.StructureByteStride = src->Buffer.StructureByteStride;// ((DX12Buffer*)pBuffer)->Desc.StructureStride;
				tar->Buffer.Flags = (D3D12_BUFFER_UAV_FLAGS)src->Buffer.Flags;
				/*if (((DX12Buffer*)pBuffer)->Desc.MiscFlags & RM_BUFFER_ALLOW_RAW_VIEWS)
					tar->Buffer.Flags = D3D12_BUFFER_UAV_FLAG_RAW;
				else
					tar->Buffer.Flags = D3D12_BUFFER_UAV_FLAG_NONE;*/
			}
			break;
			case EDimensionUAV::UAV_DIMENSION_TEXTURE1D:
			{
				tar->ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_TEXTURE1D;
				tar->Texture1D.MipSlice = src->Texture1D.MipSlice;
			}
			break;
			case EDimensionUAV::UAV_DIMENSION_TEXTURE1DARRAY:
			{
				tar->ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_TEXTURE1DARRAY;
				tar->Texture1DArray.ArraySize = src->Texture1DArray.ArraySize;
				tar->Texture1DArray.FirstArraySlice = src->Texture1DArray.FirstArraySlice;
				tar->Texture1DArray.MipSlice = src->Texture1DArray.MipSlice;
			}
			break;
			case EDimensionUAV::UAV_DIMENSION_TEXTURE2D:
			{
				tar->ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_TEXTURE2D;
				tar->Texture2D.MipSlice = src->Texture2D.MipSlice;
				tar->Texture2D.PlaneSlice = src->Texture2D.PlaneSlice;
			}
			break;
			case EDimensionUAV::UAV_DIMENSION_TEXTURE2DARRAY:
			{
				tar->ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_TEXTURE2DARRAY;
				tar->Texture2DArray.ArraySize = src->Texture2DArray.ArraySize;
				tar->Texture2DArray.FirstArraySlice = src->Texture2DArray.FirstArraySlice;
				tar->Texture2DArray.MipSlice = src->Texture2DArray.MipSlice;
				tar->Texture2DArray.PlaneSlice = src->Texture2DArray.PlaneSlice;
			}
			break;
			case EDimensionUAV::UAV_DIMENSION_TEXTURE3D:
			{
				tar->ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_TEXTURE3D;
				tar->Texture3D.MipSlice = src->Texture3D.MipSlice;
				tar->Texture3D.WSize = src->Texture3D.WSize;
				tar->Texture3D.FirstWSlice = src->Texture3D.FirstWSlice;
			}
			break;
			default:
				ASSERT(false);
				break;
		}
	}
	bool DX12UaView::Init(DX12GpuDevice* device, IGpuBufferData* pBuffer, const FUavDesc& desc)
	{
		mDeviceRef.FromObject(device);
		Desc = desc;
		Buffer = pBuffer;
		mView = device->mSrvAllocHeapManager->Alloc<DX12DescriptorSetPagedObject>();

		D3D12_UNORDERED_ACCESS_VIEW_DESC tmp{};
		UavDesc2DX(pBuffer, &tmp, &desc);
		if (desc.ViewDimension == EDimensionUAV::UAV_DIMENSION_BUFFER)
		{
			tmp.Format = DXGI_FORMAT::DXGI_FORMAT_UNKNOWN;
		}
		if (tmp.Buffer.Flags & D3D12_BUFFER_UAV_FLAGS::D3D12_BUFFER_UAV_FLAG_RAW)
		{
			tmp.Format = DXGI_FORMAT::DXGI_FORMAT_R32_TYPELESS;
			tmp.Buffer.StructureByteStride = 0;
		}
		device->mDevice->CreateUnorderedAccessView((ID3D12Resource*)pBuffer->GetHWBuffer(), nullptr, &tmp, mView->GetHandle(0));

		return true;
	}
	
	DX12RenderTargetView::DX12RenderTargetView()
	{
		mView = nullptr;
	}

	DX12RenderTargetView::~DX12RenderTargetView()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;
		device->DelayDestroy(mView);
	}

	void RtvDesc2DX(D3D12_RENDER_TARGET_VIEW_DESC* tar, const FRtvDesc* src)
	{
		memset(tar, 0, sizeof(D3D12_RENDER_TARGET_VIEW_DESC));
		tar->Format = FormatToDX12Format(src->Format);
		switch (src->Type)
		{
			case ERtvType::RTV_Buffer:
			{
				tar->Buffer.FirstElement = src->Buffer.FirstElement;
				tar->Buffer.NumElements = src->Buffer.NumElements;
				tar->ViewDimension = D3D12_RTV_DIMENSION_BUFFER;
			}
			break;
			case ERtvType::RTV_Texture1D:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE1D;
				tar->Texture1D.MipSlice = src->Texture1D.MipSlice;
			}
			break;
			case ERtvType::RTV_Texture2D:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;
				tar->Texture2D.MipSlice = src->Texture2D.MipSlice;
				tar->Texture2D.PlaneSlice = src->Texture2D.MipSlice;
			}
			break;
			case ERtvType::RTV_Texture3D:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE3D;
				memcpy(&tar->Texture3D, &src->Texture3D, sizeof(src->Texture3D));
			}
			break;
			case ERtvType::RTV_Texture1DArray:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE1DARRAY;
				tar->Texture1DArray.MipSlice = src->Texture1DArray.MipSlice;
				tar->Texture1DArray.ArraySize = src->Texture1DArray.ArraySize;
				tar->Texture1DArray.FirstArraySlice = src->Texture1DArray.FirstArraySlice;
			}
			break;
			case ERtvType::RTV_Texture2DArray:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2DARRAY;
				tar->Texture2DArray.MipSlice = src->Texture2DArray.MipSlice;
				tar->Texture2DArray.ArraySize = src->Texture2DArray.ArraySize;
				tar->Texture2DArray.FirstArraySlice = src->Texture2DArray.FirstArraySlice;
			}
			break;
			case ERtvType::RTV_Texture2DMS:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2DMS;
			}
			break;
			case ERtvType::RTV_Texture2DMSArray:
			{
				tar->ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2DMSARRAY;
			}
			break;
		}

		switch (tar->Format)
		{
		case DXGI_FORMAT_R24G8_TYPELESS:
			tar->Format = DXGI_FORMAT_R24_UNORM_X8_TYPELESS;
			break;
		case DXGI_FORMAT_R32_TYPELESS:
			tar->Format = DXGI_FORMAT_R32_FLOAT;
			break;
		case DXGI_FORMAT_R16_TYPELESS:
			tar->Format = DXGI_FORMAT_R16_UNORM;
			break;
		}
	}
	bool DX12RenderTargetView::Init(DX12GpuDevice* device, ITexture* pBuffer, const FRtvDesc* desc)
	{
		mDeviceRef.FromObject(device);
		if (desc != nullptr)
			Desc = *desc;
		GpuResource = pBuffer;
		mView = device->mRtvHeapManager->Alloc<DX12DescriptorSetPagedObject>();

		if (desc == nullptr)
		{
			device->mDevice->CreateRenderTargetView((ID3D12Resource*)pBuffer->GetHWBuffer(), nullptr, mView->GetHandle(0));
		}
		else
		{
			D3D12_RENDER_TARGET_VIEW_DESC	mDesc;
			RtvDesc2DX(&mDesc, desc);

			device->mDevice->CreateRenderTargetView((ID3D12Resource*)pBuffer->GetHWBuffer(), &mDesc, mView->GetHandle(0));
		}

		return true;
	}

	DX12DepthStencilView::DX12DepthStencilView()
	{
	}

	DX12DepthStencilView::~DX12DepthStencilView()
	{
		auto device = mDeviceRef.GetPtr();
		if (device == nullptr)
			return;
		device->DelayDestroy(mView);
	}

	bool DX12DepthStencilView::Init(DX12GpuDevice* device, ITexture* pBuffer, const FDsvDesc& desc)
	{
		mDeviceRef.FromObject(device);
		Desc = desc;
		auto dxPixelFormat = FormatToDX12Format(desc.Format);
		GpuResource = pBuffer;
		mView = device->mDsvHeapManager->Alloc<DX12DescriptorSetPagedObject>();
		
		D3D12_DEPTH_STENCIL_VIEW_DESC	DSVDesc;
		memset(&DSVDesc, 0, sizeof(DSVDesc));
		switch (dxPixelFormat)
		{
		case DXGI_FORMAT_D24_UNORM_S8_UINT:
		case DXGI_FORMAT_D32_FLOAT:
		case DXGI_FORMAT_D16_UNORM:
			DSVDesc.Format = dxPixelFormat;
			break;
		default:
			DSVDesc.Format = DXGI_FORMAT_D32_FLOAT;
			break;
		}
		DSVDesc.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D;
		
		device->mDevice->CreateDepthStencilView((ID3D12Resource*)pBuffer->GetHWBuffer(), &DSVDesc, mView->GetHandle(0));

		return true;
	}
}
NS_END