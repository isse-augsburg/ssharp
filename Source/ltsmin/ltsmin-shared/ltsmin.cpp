// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

//---------------------------------------------------------------------------------------------------------------------------
// C standard library includes
//---------------------------------------------------------------------------------------------------------------------------
#include <cstdlib>
#include <cstdint>
#include <cstdio>
#include <cstring>

//---------------------------------------------------------------------------------------------------------------------------
// LtsMin includes
//---------------------------------------------------------------------------------------------------------------------------
extern "C" 
{
	#pragma warning(push)
	#pragma warning(disable: 4200)
		#include "chunk-support.h"
		#include "string-map.h"
		#include "bitvector.h"
		#include "dm.h"
		#include "lts-type.h"
		#include "ltsmin-standard.h"
		#include "pins.h"
	#pragma warning(pop)
}

//---------------------------------------------------------------------------------------------------------------------------
// S# includes
//---------------------------------------------------------------------------------------------------------------------------
#using "SafetySharp.Modeling.dll" as_friend

//---------------------------------------------------------------------------------------------------------------------------
// Namespace imports
//---------------------------------------------------------------------------------------------------------------------------
using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace SafetySharp::Analysis::StateSpace;

//---------------------------------------------------------------------------------------------------------------------------
// Assembly metadata
//---------------------------------------------------------------------------------------------------------------------------
[assembly: AssemblyTitle("S# LtsMin Interop")];
[assembly: AssemblyDescription("S# LtsMin Interop")];
[assembly: AssemblyCompany("Institute for Software & Systems Engineering")];
[assembly: AssemblyProduct("S#")];
[assembly: AssemblyCopyright("Copyright (c) 2014-2015, Institute for Software & Systems Engineering")];
[assembly: AssemblyCulture("")];
[assembly: AssemblyVersion("0.1.0.0")];
[assembly: AssemblyFileVersion("0.1.0.0")];
[assembly: ComVisible(false)];

//---------------------------------------------------------------------------------------------------------------------------
// Forward declarations
//---------------------------------------------------------------------------------------------------------------------------
void LoadModel(model_t model, const char* file);
int32_t NextStatesCallback(model_t model, int32_t group, int32_t* state, TransitionCB callback, void* context);
int32_t StateLabelCallback(model_t model, int32_t label, int32_t* state);

//---------------------------------------------------------------------------------------------------------------------------
// Delegate declarations
//---------------------------------------------------------------------------------------------------------------------------
[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate StateCache^ NextStatesDelegate(int32_t* state, int32_t transitionGroup);

[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate bool StateLabelDelegate(int32_t* state, int32_t label);

//---------------------------------------------------------------------------------------------------------------------------
// Global variables
//---------------------------------------------------------------------------------------------------------------------------
matrix_t CombinedMatrix;
matrix_t ReadMatrix;
matrix_t WriteMatrix;
matrix_t StateLabelMatrix;

// Global variables of managed types must be wrapped in a class...
ref struct Globals 
{
	static NextStatesDelegate^ NextStates;
	static StateLabelDelegate^ StateLabel;
};

//---------------------------------------------------------------------------------------------------------------------------
// PINS exports
//---------------------------------------------------------------------------------------------------------------------------
extern "C" __declspec(dllexport) char pins_plugin_name[] = "S# Model";
extern "C" __declspec(dllexport) loader_record pins_loaders[] = { {"ssharp", LoadModel}, { nullptr, nullptr } };
extern "C" __declspec(dllexport) void* pins_options[] = { nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr };

//---------------------------------------------------------------------------------------------------------------------------
// S# model loading
//---------------------------------------------------------------------------------------------------------------------------
void LoadModel(model_t model, const char* file)
{
	auto modelInfo = ModelInfo::Load(gcnew String(file));
	auto stateSpace = gcnew StateSpaceEnumerator(modelInfo);

	Globals::NextStates = gcnew NextStatesDelegate(stateSpace, &StateSpaceEnumerator::ComputeNextStates);
	Globals::StateLabel = gcnew StateLabelDelegate(stateSpace, &StateSpaceEnumerator::CheckStateLabel);

	// Create the LTS type and set the state vector size
	auto ltsType = lts_type_create();
	lts_type_set_state_length(ltsType, stateSpace->StateVectorSize);
	Console::WriteLine("State vector has {0} slots ({1} bytes).", stateSpace->StateVectorSize, stateSpace->StateVectorSize * sizeof(int32_t));

	// Set the 'int' type for state slots
	auto intType = lts_type_put_type(ltsType, "int", LTStypeDirect, nullptr);
	for (auto i = 0; i < stateSpace->StateVectorSize; ++i)
	{
		char name[10];
		sprintf_s(name, "state%d", i);
		lts_type_set_state_name(ltsType, i, name);
		lts_type_set_state_typeno(ltsType, i, intType);
	}

	// Create the state labels
	auto boolType = lts_type_put_type(ltsType, LTSMIN_TYPE_BOOL, LTStypeEnum, nullptr);
	auto stateLabel = Marshal::StringToHGlobalAnsi(modelInfo->InvariantStateLabel);
	lts_type_set_state_label_count(ltsType, 1);
	lts_type_set_state_label_name(ltsType, 0, (char*)stateLabel.ToPointer());
	lts_type_set_state_label_typeno(ltsType, 0, boolType);
	Marshal::FreeHGlobal(stateLabel);

	// Finalize the LTS type and set it for the model
	lts_type_validate(ltsType);
	GBsetLTStype(model, ltsType);

	// Assign enum names
	GBchunkPut(model, boolType, chunk_str(LTSMIN_VALUE_BOOL_FALSE));
	GBchunkPut(model, boolType, chunk_str(LTSMIN_VALUE_BOOL_TRUE));

	// Set the initial state, the user context, and the callback functions
	GBsetInitialState(model, stateSpace->GetInitialState());
	GBsetNextStateLong(model, NextStatesCallback);
	GBsetStateLabelLong(model, StateLabelCallback);

	// Create the dependency matrices
	dm_create(&CombinedMatrix, stateSpace->TransitionGroupCount, stateSpace->StateVectorSize);
	dm_create(&ReadMatrix, stateSpace->TransitionGroupCount, stateSpace->StateVectorSize);
	dm_create(&WriteMatrix, stateSpace->TransitionGroupCount, stateSpace->StateVectorSize);
	dm_create(&StateLabelMatrix, /* TODO: Label count */ 1, stateSpace->StateVectorSize);

	// Initialize the dependency matrices
	for (int i = 0; i < stateSpace->TransitionGroupCount; i++)
	{
		for (int j = 0; j < stateSpace->StateVectorSize; j++)
		{
			dm_set(&CombinedMatrix, i, j);
			dm_set(&ReadMatrix, i, j);
			dm_set(&WriteMatrix, i, j);
			dm_set(&StateLabelMatrix, i, j);
		}
	}

	// Set the dependency matrices
	GBsetDMInfo(model, &CombinedMatrix);
	GBsetDMInfoRead(model, &ReadMatrix);
	GBsetDMInfoMustWrite(model, &WriteMatrix);
	GBsetStateLabelInfo(model, &StateLabelMatrix);
}

//---------------------------------------------------------------------------------------------------------------------------
// Next state function
//---------------------------------------------------------------------------------------------------------------------------
int32_t NextStatesCallback(model_t model, int32_t group, int32_t* state, TransitionCB callback, void* context)
{
	(void)model;

	auto stateCache = Globals::NextStates(state, group);
	auto stateCount = stateCache->StateCount;
	auto stateSize = stateCache->SlotCount;
	auto stateMemory = stateCache->StateMemory;

	transition_info info = { nullptr, 0, 0 };
	for (auto i = 0; i < stateCount; ++i)
	{
		callback(context, &info, stateMemory, nullptr);
		stateMemory += stateSize;
	}

	return stateCount;
}

//---------------------------------------------------------------------------------------------------------------------------
// State label function
//---------------------------------------------------------------------------------------------------------------------------
int32_t StateLabelCallback(model_t model, int32_t label, int32_t* state)
{
	(void)model;

	return Globals::StateLabel(state, label) ? 1 : 0;
}