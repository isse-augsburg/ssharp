//---------------------------------------------------------------------------------------------------------------------------
// C standard library includes
//---------------------------------------------------------------------------------------------------------------------------
#include <cstdlib>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <utility>

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
// Helper types, functions, and macros
//---------------------------------------------------------------------------------------------------------------------------
#include <windows.h>

HMODULE GetLtsMinExecutable() 
{
	static HMODULE executable = LoadLibrary(L"pins2lts-seq.exe");
	return executable;
}

template <typename T>
class Delegate
{
};

template <typename TReturn, typename... TArgs>
class Delegate<TReturn(TArgs...)>
{
public:
	Delegate(const char* entryPoint)
	{
		_func = reinterpret_cast<Func>(GetProcAddress(GetLtsMinExecutable(), entryPoint));
	}

	TReturn operator()(TArgs... args) 
	{
		return _func(std::forward<TArgs>(args)...);
	}

private:
	using Func = TReturn(*)(TArgs...);
	Func _func;
};

#define FUNC(name) static Delegate<decltype(name)> func(#name);

//---------------------------------------------------------------------------------------------------------------------------
// Functions
//---------------------------------------------------------------------------------------------------------------------------
void lts_type_validate(lts_type_s* p)
{
	FUNC(lts_type_validate);
	func(p);
}

void GBsetDMInfo(grey_box_model* p1, matrix* p2)
{
	FUNC(GBsetDMInfo);
	func(p1, p2);
}

void GBsetDMInfoMustWrite(grey_box_model* p1, matrix* p2)
{
	FUNC(GBsetDMInfoMustWrite);
	func(p1, p2);
}

void GBsetLTStype(grey_box_model* p1, lts_type_s* p2)
{
	FUNC(GBsetLTStype);
	func(p1, p2);
}

void lts_type_set_state_name(lts_type_s* p1, int p2, char const* p3)
{
	FUNC(lts_type_set_state_name);
	func(p1, p2, p3);
}

int lts_type_put_type(lts_type_s* p1, char const* p2, data_format_t p3, int* p4)
{
	FUNC(lts_type_put_type);
	return func(p1, p2, p3, p4);
}

void lts_type_set_state_label_name(lts_type_s* p1, int p2, char const* p3)
{
	FUNC(lts_type_set_state_label_name);
	func(p1, p2, p3);
}

void lts_type_set_state_typeno(lts_type_s* p1, int p2, int p3)
{
	FUNC(lts_type_set_state_typeno);
	func(p1, p2, p3);
}

void GBsetNextStateLong(grey_box_model* p1, int (*p2)(grey_box_model*, int, int*, void(*)(void*, transition_info*, int*, int*), void*))
{
	FUNC(GBsetNextStateLong);
	func(p1, p2);
}

void GBsetStateLabelLong(grey_box_model* p1, int (*p2)(grey_box_model*, int, int*))
{
	FUNC(GBsetStateLabelLong);
	func(p1, p2);
}

void dm_set(matrix* p1, int p2, int p3)
{
	FUNC(dm_set);
	func(p1, p2, p3);
}

int dm_create(matrix* p1, int p2, int p3)
{
	FUNC(dm_create);
	return func(p1, p2, p3);
}

void GBsetStateLabelInfo(grey_box_model* p1, matrix* p2)
{
	FUNC(GBsetStateLabelInfo);
	func(p1, p2);
}

lts_type_s* lts_type_create()
{
	FUNC(lts_type_create);
	return func();
}

int GBchunkPut(grey_box_model* p1, int p2, chunk p3)
{
	FUNC(GBchunkPut);
	return func(p1, p2, p3);
}

void lts_type_set_state_length(lts_type_s* p1, int p2)
{
	FUNC(lts_type_set_state_length);
	func(p1, p2);
}

void lts_type_set_state_label_typeno(lts_type_s* p1, int p2, int p3)
{
	FUNC(lts_type_set_state_label_typeno);
	func(p1, p2, p3);
}

void GBsetDMInfoRead(grey_box_model* p1, matrix* p2)
{
	FUNC(GBsetDMInfoRead);
	func(p1, p2);
}

void lts_type_set_state_label_count(lts_type_s* p1, int p2)
{
	FUNC(lts_type_set_state_label_count);
	func(p1, p2);
}

void ltsmin_abort(int p)
{
	FUNC(ltsmin_abort);
	func(p);
}

void GBsetInitialState(grey_box_model* p1, int* p2)
{
	FUNC(GBsetInitialState);
	func(p1, p2);
}