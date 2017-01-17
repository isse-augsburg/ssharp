// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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
#using "ISSE.ModelChecking.dll" as_friend


//---------------------------------------------------------------------------------------------------------------------------
// Namespace imports
//---------------------------------------------------------------------------------------------------------------------------
using namespace System;
using namespace System::IO;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;
using namespace SafetySharp::Analysis;
using namespace SafetySharp::Analysis::ModelChecking;
using namespace SafetySharp::Analysis::ModelChecking::Transitions;
using namespace SafetySharp::Runtime;
using namespace SafetySharp::Runtime::Serialization;
using namespace ISSE::ModelChecking::ExecutableModel;

//---------------------------------------------------------------------------------------------------------------------------
// Assembly metadata
//---------------------------------------------------------------------------------------------------------------------------
[assembly:AssemblyTitle("S# LtsMin Interop")];
[assembly:AssemblyDescription("S# LtsMin Interop")];
[assembly:AssemblyCompany("Institute for Software & Systems Engineering")];
[assembly:AssemblyProduct("S#")];
[assembly:AssemblyCopyright("Copyright (c) 2014-2016, Institute for Software & Systems Engineering")];
[assembly:AssemblyCulture("")];
[assembly:ComVisible(false)];

//---------------------------------------------------------------------------------------------------------------------------
// Forward declarations
//---------------------------------------------------------------------------------------------------------------------------
void PrepareLoadModel(model_t model, const char* file);
void LoadModel(model_t model, const char* file);
int32_t NextStatesCallback(model_t model, int32_t group, int32_t* state, TransitionCB callback, void* context);
int32_t StateLabelCallback(model_t model, int32_t label, int32_t* state);
bool IsConstructionState(int32_t* state);
Assembly^ OnAssemblyResolve(Object^ o, ResolveEventArgs^ e);

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
	static ActivationMinimalExecutedModel<SafetySharpRuntimeModel^>^ ExecutedModel;
	static SafetySharpRuntimeModel^ RuntimeModel;
	static LtsMin^ LtsMin;
	static const char* ModelFile;
};

//---------------------------------------------------------------------------------------------------------------------------
// PINS exports
//---------------------------------------------------------------------------------------------------------------------------
extern "C" __declspec(dllexport) char pins_plugin_name[] = "S# Model";
extern "C" __declspec(dllexport) loader_record pins_loaders[] = { { "ssharp", PrepareLoadModel },{ nullptr, nullptr } };
extern "C" __declspec(dllexport) void* pins_options[] = { nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr };

//---------------------------------------------------------------------------------------------------------------------------
// S# model loading
//---------------------------------------------------------------------------------------------------------------------------
void PrepareLoadModel(model_t model, const char* modelFile)
{
	AppDomain::CurrentDomain->AssemblyResolve += gcnew System::ResolveEventHandler(&OnAssemblyResolve);
	LoadModel(model, modelFile);
}

SafetySharpRuntimeModel^ CreateModel()
{
	return Globals::RuntimeModel;
}

CoupledExecutableModelCreator<SafetySharpRuntimeModel^>^ CreateModelCreator()
{
	auto createModelFunc = gcnew Func<SafetySharpRuntimeModel^>(&CreateModel);
	auto model = Globals::RuntimeModel->Model;
	auto formulas = Globals::RuntimeModel->Formulas;
	auto creator = gcnew CoupledExecutableModelCreator<SafetySharpRuntimeModel^>(createModelFunc,model,formulas);
	return creator;
}


void LoadModel(model_t model, const char* modelFile)
{
	try
	{
		auto modelData = RuntimeModelSerializer::LoadSerializedData(File::ReadAllBytes(gcnew String(modelFile)));
		Globals::RuntimeModel = gcnew SafetySharpRuntimeModel(modelData, sizeof(int32_t));
		Globals::ExecutedModel = gcnew ActivationMinimalExecutedModel<SafetySharpRuntimeModel^>(CreateModelCreator(), gcnew array<Func<bool>^>(0), 1 << 16);

		auto stateSlotCount = (int32_t)(Globals::RuntimeModel->StateVectorSize / sizeof(int32_t));
		auto stateLabelCount = Globals::RuntimeModel->ExecutableStateFormulas->Length;
		Console::WriteLine("State Labels: "+stateLabelCount);
		auto transitionGroupCount = 1;

		// Create the LTS type and set the state vector size
		auto ltsType = lts_type_create();
		lts_type_set_state_length(ltsType, stateSlotCount);
		Console::WriteLine("State vector has {0} slots ({1} bytes).", stateSlotCount, stateSlotCount * sizeof(int32_t));

		// Set the 'int' type for state slots and their names
		auto intType = lts_type_put_type(ltsType, "int", LTStypeDirect, nullptr);
		for (auto i = 0; i < stateSlotCount; ++i)
		{
			lts_type_set_state_typeno(ltsType, i, intType);

			// Slot 0 is the special pseudo construction slot
			if (i == 0)
			{
				auto name = Marshal::StringToHGlobalAnsi(LtsMin::ConstructionStateName);
				lts_type_set_state_name(ltsType, i, (char*)name.ToPointer());
				Marshal::FreeHGlobal(name);
			}
			else 
			{
				char name[10];
				sprintf_s(name, "state%d", i);
				lts_type_set_state_name(ltsType, i, name);
			}
		}

		// Create the state labels
		auto boolType = lts_type_put_type(ltsType, LTSMIN_TYPE_BOOL, LTStypeEnum, nullptr);
		lts_type_set_state_label_count(ltsType, stateLabelCount);

		for (auto i = 0; i < stateLabelCount; ++i)
		{
			auto stateLabel = Marshal::StringToHGlobalAnsi(Globals::RuntimeModel->ExecutableStateFormulas[i]->Label);
			auto name = (char*)stateLabel.ToPointer();
			Console::WriteLine("State Label " + i + ": "+ (gcnew System::String(name)));
			lts_type_set_state_label_name(ltsType, i, (char*)stateLabel.ToPointer());
			lts_type_set_state_label_typeno(ltsType, i, boolType);
			Marshal::FreeHGlobal(stateLabel);
		}

		// Finalize the LTS type and set it for the model
		lts_type_validate(ltsType);
		GBsetLTStype(model, ltsType);

		// Assign enum names
		GBchunkPut(model, boolType, chunk_str(LTSMIN_VALUE_BOOL_FALSE));
		GBchunkPut(model, boolType, chunk_str(LTSMIN_VALUE_BOOL_TRUE));
		
		// Set the initial state, the user context, and the callback functions
		pin_ptr<unsigned char> initialStatePtr = &Globals::RuntimeModel->ConstructionState[0];
		auto initialState = (int32_t*)initialStatePtr;
		initialState[0] = 1;
		GBsetInitialState(model, initialState);
		GBsetNextStateLong(model, NextStatesCallback);
		GBsetStateLabelLong(model, StateLabelCallback);

		// Create the dependency matrices
		dm_create(&CombinedMatrix, transitionGroupCount, stateSlotCount);
		dm_create(&ReadMatrix, transitionGroupCount, stateSlotCount);
		dm_create(&WriteMatrix, transitionGroupCount, stateSlotCount);
		dm_create(&StateLabelMatrix, stateLabelCount, stateSlotCount);

		// Initialize the dependency matrices
		for (int i = 0; i < transitionGroupCount; i++)
		{
			for (int j = 0; j < stateSlotCount; j++)
			{
				dm_set(&CombinedMatrix, i, j);
				dm_set(&ReadMatrix, i, j);
				dm_set(&WriteMatrix, i, j);
			}
		}

		// Initialize the state label matrix
		for (int i = 0; i < stateLabelCount; i++)
		{
			for (int j = 0; j < stateSlotCount; j++)
				dm_set(&StateLabelMatrix, i, j);
		}

		// Set the matrices
		GBsetDMInfo(model, &CombinedMatrix);
		GBsetDMInfoRead(model, &ReadMatrix);
		GBsetDMInfoMustWrite(model, &WriteMatrix);
		GBsetStateLabelInfo(model, &StateLabelMatrix);
	}
	catch (Exception^ e)
	{
		Console::WriteLine(e);
		ltsmin_abort(255);
	}
}

//---------------------------------------------------------------------------------------------------------------------------
// Next states function
//---------------------------------------------------------------------------------------------------------------------------
int32_t NextStatesCallback(model_t model, int32_t group, int32_t* state, TransitionCB callback, void* context)
{
	(void)model;
	(void)group;

	try
	{
		auto transitions = IsConstructionState(state)
			? Globals::ExecutedModel->GetInitialTransitions()
			: Globals::ExecutedModel->GetSuccessorTransitions((unsigned char*)state);

		transition_info info = { nullptr, 0, 0 };
		auto transitionCount = 0;

		for each (auto transition in transitions)
		{
			auto stateMemory = (int32_t*)((CandidateTransition*)transition)->TargetState;
			stateMemory[0] = 0;
			callback(context, &info, stateMemory, nullptr);

			++transitionCount;
		}

		return transitionCount;
	}
	catch (Exception^ e)
	{
		Console::WriteLine(e);
		ltsmin_abort(255);

		return 0;
	}
}

//---------------------------------------------------------------------------------------------------------------------------
// State label function
//---------------------------------------------------------------------------------------------------------------------------
int32_t StateLabelCallback(model_t model, int32_t label, int32_t* state)
{
	(void)model;

	try
	{
		Globals::RuntimeModel->Deserialize((unsigned char*)state);
		return Globals::RuntimeModel->ExecutableStateFormulas[label]->Expression() ? 1 : 0;
	}
	catch (Exception^ e)
	{
		Console::WriteLine(e);
		ltsmin_abort(255);

		return 0;
	}
}

//---------------------------------------------------------------------------------------------------------------------------
// Construction State Check
//---------------------------------------------------------------------------------------------------------------------------
bool IsConstructionState(int32_t* state)
{
	return state[0] == 1;
}

//---------------------------------------------------------------------------------------------------------------------------
// Assembly resolving
//---------------------------------------------------------------------------------------------------------------------------
Assembly^ OnAssemblyResolve(Object^, ResolveEventArgs^ e)
{
	auto fileName = Path::Combine(Environment::CurrentDirectory, AssemblyName(e->Name).Name);

	if (File::Exists(fileName + ".dll"))
		return Assembly::LoadFile(fileName + ".dll");
	
	if (File::Exists(fileName + ".exe"))
		return Assembly::LoadFile(fileName + ".exe");

	return nullptr;
}