#include "NxInputAssembly.h"

#define new VNEW

NS_BEGIN

namespace NxRHI
{
	UINT64 FInputLayoutDesc::GetLayoutHash64()
	{
		std::string hashStr = "";
		for (size_t i = 0; i < Layouts.size(); i++)
		{
			hashStr += VStringA_FormatV("%s_%d_%d_%d_%d_%d_%d",
				Layouts[i].SemanticName.c_str(),
				Layouts[i].SemanticIndex,
				Layouts[i].Format,
				Layouts[i].InputSlot,
				Layouts[i].AlignedByteOffset,
				Layouts[i].IsInstanceData,
				Layouts[i].InstanceDataStepRate);
		}
		return HashHelper::CalcHash64(hashStr.c_str(), (int)hashStr.length()).Int64Value;
	}
}

NS_END