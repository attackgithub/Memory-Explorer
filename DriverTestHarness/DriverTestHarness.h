#pragma once
#include "winioctl.h"

#define PMEM_CTRL_IOCTRL CTL_CODE(0x22, 0x101, 0, 3)
#define PMEM_WRITE_ENABLE CTL_CODE(0x22, 0x102, 0, 3)
#define PMEM_INFO_IOCTRL CTL_CODE(0x22, 0x103, 0, 3)

#define PMEM_MODE_IOSPACE 0
#define PMEM_MODE_PHYSICAL 1
#define PMEM_MODE_PTE 2
#define PMEM_MODE_PTE_PCI 3
#define PMEM_MODE_AUTO 99

#pragma pack(push, 2)
typedef struct pmem_info_runs {
	__int64 start;
	__int64 length;
} PHYSICAL_MEMORY_RANGE;

struct PmemMemoryInfo {
	LARGE_INTEGER CR3;
	LARGE_INTEGER NtBuildNumber;
	LARGE_INTEGER KernBase;
	LARGE_INTEGER KDBG;
	LARGE_INTEGER KPCR[32];
	LARGE_INTEGER PfnDataBase;
	LARGE_INTEGER PsLoadedModuleList;
	LARGE_INTEGER PsActiveProcessHead;
	LARGE_INTEGER NtBuildNumberAddr;
	LARGE_INTEGER Padding[0xfe];
	LARGE_INTEGER NumberOfRuns;
	PHYSICAL_MEMORY_RANGE Run[100];
};
#pragma pack(pop)

BOOL RegisterDriver(LPCTSTR szDriverName, LPCTSTR szPathName);
BOOL StartDriver(LPCTSTR szDriverName);
BOOL StopDriver(LPCTSTR szDriverName);
BOOL UnregisterDriver(LPCTSTR szDriverName);