#include "NxGpuDevice.h"
#include "NxGpuState.h"
#include "NxGeomMesh.h"
#include "NxDrawcall.h"
#include "NullDevice/NullGpuDevice.h"

#if defined(HasModule_RenderDoc)
#include "../Bricks/RenderDoc/IRenderDocTool.h"
#endif

#if defined(HasModule_Dx11)
#include "Dx11/DX11GpuDevice.h"
#endif

#if defined(HasModule_Dx12)
#include "Dx12/DX12GpuDevice.h"
#endif

#if defined(HasModule_Vulkan)
#include "Vulkan/VKGpuDevice.h"
#endif

#define new VNEW

NS_BEGIN

namespace NxRHI
{
	/*struct TT
	{
		static int AAAA;
	};

	struct TT2
	{

	};

	template<class _Ty, class _Checker>
	struct checkvar : std::false_type {};

	template<class _Ty>
	struct checkvar<_Ty, decltype(_Ty::AAAA)> : std::true_type {};

	int xxx1 = 0;
	int xxx2 = 0;
	void TTTTTTTTTTTTTT()
	{
		if constexpr (checkvar<TT, int>::value)
		{
			xxx1 = 1;
		}
		if constexpr (checkvar<int, int>::value)
		{
			xxx2 = 1;
		}
	}*/
	IGpuSystem* IGpuSystem::CreateGpuSystem(ERhiType type, const FGpuSystemDesc* desc)
	{
		if (desc->UseRenderDoc)
		{
#if defined(HasModule_RenderDoc)
			IRenderDocTool::GetInstance()->InitRenderDoc();
#endif
		}
		switch (type)
		{
			case EngineNS::NxRHI::RHI_D3D11:
			{
#if defined(HasModule_Dx11)
				auto result = new DX11GpuSystem();
				result->Type = type;
				result->InitGpuSystem(type, desc);
				return result;
#else
				return nullptr;
#endif
			}
			case EngineNS::NxRHI::RHI_D3D12:
			{
#if defined(HasModule_Dx12)
				auto result = new DX12GpuSystem();
				result->Type = type;
				result->InitGpuSystem(type, desc);
				return result;
#else
				ASSERT(false);
				return nullptr;
#endif
			}
			case EngineNS::NxRHI::RHI_VK:
			{
#if defined(HasModule_Vulkan)
				auto result = new VKGpuSystem();
				result->Type = type;
				result->InitGpuSystem(type, desc);
				return result;
#else
				ASSERT(false);
				return nullptr;
#endif
			}
			case EngineNS::NxRHI::RHI_GL:
				break;
			case EngineNS::NxRHI::RHI_VirtualDevice:
			{
#if defined(HasModule_NullDevice)
				auto result = new NullGpuSystem();
				result->Type = type;
				result->InitGpuSystem(type, desc);
				return result;
#else
				ASSERT(false);
				return nullptr;
#endif
			}
			default:
				break;
		}
		return nullptr;
	}
	IGpuDevice::IGpuDevice()
	{
		mPipelineManager = MakeWeakRef(new FGpuPipelineManager());
	}	
	FGpuPipelineManager* IGpuDevice::GetGpuPipelineManager()
	{
		return mPipelineManager;
	}
	IGraphicDraw* IGpuDevice::CreateGraphicDraw()
	{
		auto result = new IGraphicDraw();
		return result;
	}
	IComputeDraw* IGpuDevice::CreateComputeDraw()
	{
		auto result = new IComputeDraw();
		return result;
	}
	ICopyDraw* IGpuDevice::CreateCopyDraw()
	{
		auto result = new ICopyDraw();
		return result;
	}
	void IGpuDevice::TickPostEvents()
	{
		if (mFrameFence != nullptr)
		{
			{
				VAutoVSLLock lk(mPostEventLocker);
				mTickingPostEvents.insert(mTickingPostEvents.begin(), mPostEvents.begin(), mPostEvents.end());
				mPostEvents.clear();
			}

			auto completed = mFrameFence->GetCompletedValue();
			for (size_t i = 0; i < mTickingPostEvents.size(); i++)
			{
				if (mTickingPostEvents[i](this, completed))
				{
					mTickingPostEvents.erase(mTickingPostEvents.begin() + i);
					i--;
				}
			}
			ASSERT(completed <= mFrameFence->GetAspectValue());
			GetCmdQueue()->IncreaseSignal(mFrameFence);
			
			//if (completed < mFrameFence->GetAspectValue())
			//{
			//	//mFrameFence->Wait(mFrameFence->GetAspectValue());
			//	GetCmdQueue()->IncreaseSignal(mFrameFence);
			//}
			//else
			//{
			//	GetCmdQueue()->IncreaseSignal(mFrameFence);
			//}
		}
	}
}

NS_END
