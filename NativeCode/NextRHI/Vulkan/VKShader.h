#pragma once
#include "../NxShader.h"
#include "VKPreHead.h"
#include "../../Base/allocator/PagedAllocator.h"

NS_BEGIN

namespace NxRHI
{
	class VKGpuDevice;
	class VKShader;
	struct VKDescriptorSetPagedObject : public MemAlloc::FPagedObject<VkDescriptorSet>
	{

	};
	template<>
	struct AuxGpuResourceDestroyer<AutoRef<VKDescriptorSetPagedObject>>
	{
		static void Destroy(AutoRef<VKDescriptorSetPagedObject> obj, IGpuDevice* device1)
		{
			//auto device = (VKGpuDevice*)device1;
			//vkDestroyBuffer(device->mDevice, obj, device->GetVkAllocCallBacks());
			// auto pAllocator = (FDescriptorSetAllocator*)obj->HostPage.GetPtr()->Allocator.GetPtr();
			//pAllocator->Creator.OnFree(obj);
			obj->Free();
		}
	};
	struct VKDescriptorSetPage : public MemAlloc::FPage<VkDescriptorSet>
	{
		VkDescriptorPool				mDescriptorPool = (VkDescriptorPool)nullptr;
	};
	struct VKDescriptorSetCreator
	{
		UINT		NumOfUbo = 0;
		UINT		NumOfSsbo = 0;
		UINT		NumOfSampler = 0;
		UINT		NumOfImage = 0;
		UINT		NumOfStorageImage = 0;

		TObjectHandle<VKGpuDevice>		mDeviceRef;
		VKShader*						Shader = nullptr;
		MemAlloc::FPage<VkDescriptorSet>* CreatePage(UINT pageSize);
		MemAlloc::FPagedObject<VkDescriptorSet>* CreatePagedObject(MemAlloc::FPage<VkDescriptorSet>* page, UINT index);
		void OnFree(MemAlloc::FPagedObject<VkDescriptorSet>* obj);
		void FinalCleanup(MemAlloc::FPage<VkDescriptorSet>* page);
	};
	class FDescriptorSetAllocator : public MemAlloc::FPagedObjectAllocator<VkDescriptorSet, VKDescriptorSetCreator, 128>
	{

	};

	class VKShader : public IShader
	{
	public:
		static bool CompileShader(FShaderCompiler* compiler, FShaderDesc* desc, const char* shader, const char* entry, EShaderType type, const char* sm, const IShaderDefinitions* defines, EShaderLanguage sl, bool bDebugShader);

		VKShader();
		~VKShader();
		bool Init(VKGpuDevice* device, FShaderDesc* desc);
		static bool Reflect(FShaderDesc* desc);
	public:
		TObjectHandle<VKGpuDevice>	mDeviceRef;
		VkShaderModule		mShader{};
		VNameString			mFunctionName;
		std::vector<VkDescriptorSetLayoutBinding>	mLayoutBindings;
		VkDescriptorSetLayout				mLayout = (VkDescriptorSetLayout)nullptr;
		FDescriptorSetAllocator				mDescriptorSetAllocator;
	};
}

NS_END