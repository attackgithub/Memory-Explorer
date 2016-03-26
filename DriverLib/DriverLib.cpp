// DriverLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "DriverLib.h"
#include <stdexcept>

using namespace std;

namespace GuardianAngel
{
	int Version()
	{
		return 1;
	}
	int ReadPage(ULONG64 StartAddress, unsigned char buffer[])
	{
		HANDLE hDevice;
		ULONG bytesReturned;
		LARGE_INTEGER largeStart;

		hDevice = CreateFileA("\\\\.\\pmem", GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		if (hDevice == INVALID_HANDLE_VALUE)
			return 1;
		largeStart.QuadPart = StartAddress;
		if (0xFFFFFFFF == SetFilePointerEx(hDevice, largeStart, NULL, FILE_BEGIN))
		{
			CloseHandle(hDevice);
			return 3;
		}
		if (!ReadFile(hDevice, buffer, 4096, &bytesReturned, NULL))
		{
			CloseHandle(hDevice);
			return 4;
		}
		CloseHandle(hDevice);
		return 0;
	}
	int GetInfo(unsigned char buffer[])
	{
		HANDLE hDevice;
		DWORD size;
		struct PmemMemoryInfo info;
		unsigned int mode;

		hDevice = CreateFileA("\\\\.\\pmem", GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		if (hDevice == INVALID_HANDLE_VALUE)
			return 1;
		if (!DeviceIoControl(hDevice, PMEM_INFO_IOCTRL, NULL, 0, (char*)&info, sizeof(info), &size, NULL))
		{
			CloseHandle(hDevice);
			return 2;
		}
		mode = PMEM_MODE_PTE;
		if (!DeviceIoControl(hDevice, PMEM_CTRL_IOCTRL, &mode, 4, NULL, 0, &size, NULL))
		{
			CloseHandle(hDevice);
			return 3;
		}
		CloseHandle(hDevice);
		memcpy(buffer, &info, sizeof(info));
		return 0;
	}
	//
	//	RegisterDriver
	//
	//	Registers the specified device-driver
	//
	BOOL RegisterDriver(LPCTSTR szDriverName, LPCTSTR szPathName)
	{
		SC_HANDLE	hSCManager;
		SC_HANDLE	hService;

		if (!(hSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS)))
			return FALSE;

		hService = CreateService(hSCManager,				// SCManager database
			szDriverName,			// name of service
			szDriverName,			// name to display
			SERVICE_ALL_ACCESS,		// desired access
			SERVICE_KERNEL_DRIVER,	// service type
			SERVICE_DEMAND_START,	// start type = MANUAL
			SERVICE_ERROR_NORMAL,	// error control type
			szPathName,				// service's binary
			NULL,					// no load ordering group
			NULL,					// no tag identifier
			NULL,					// no dependencies
			NULL,					// LocalSystem account
			NULL					// no password
			);

		if (hService)
		{
			CloseServiceHandle(hService);
			CloseServiceHandle(hSCManager);
			return TRUE;
		}
		else
		{
			BOOL fStatus = GetLastError();

			CloseServiceHandle(hSCManager);

			// if driver is already registered then this is OK
			return fStatus == ERROR_SERVICE_EXISTS;
		}
	}
	//
	//	StartDriver
	//
	//	Starts (Loads) the specified driver
	//
	BOOL StartDriver(LPCTSTR szDriverName)
	{
		SC_HANDLE	hSCManager;
		SC_HANDLE	hService;
		BOOL		fStatus;

		if (!(hSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS)))
			return 0;

		if (!(hService = OpenService(hSCManager, szDriverName, SERVICE_ALL_ACCESS)))
		{
			CloseServiceHandle(hSCManager);
			return FALSE;
		}

		// start the driver
		if (!(fStatus = StartService(hService, 0, NULL)))
		{
			// if already running then this is OK!!
			if (GetLastError() == ERROR_SERVICE_ALREADY_RUNNING)
				fStatus = TRUE;
		}

		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);

		return fStatus;
	}
	//
	//	StopDriver
	//
	//	Stops (unloads) the specified driver
	//
	BOOL StopDriver(LPCTSTR szDriverName)
	{
		SC_HANDLE		hSCManager;
		SC_HANDLE		hService;
		BOOL			fStatus;
		SERVICE_STATUS  serviceStatus;

		if (!(hSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS)))
			return 0;

		if (!(hService = OpenService(hSCManager, szDriverName, SERVICE_ALL_ACCESS)))
		{
			CloseServiceHandle(hSCManager);
			return FALSE;
		}

		if (!(fStatus = ControlService(hService, SERVICE_CONTROL_STOP, &serviceStatus)))
		{
			if (GetLastError() == ERROR_SERVICE_NOT_ACTIVE)
				fStatus = TRUE;
		}

		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);

		return fStatus;
	}
	//
	//	UnregisterDriver
	//
	//	Unregisters the specified driver
	//
	BOOL UnregisterDriver(LPCTSTR szDriverName)
	{
		SC_HANDLE  hService;
		SC_HANDLE  hSCManager;
		BOOL       fStatus;

		if (!(hSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS)))
			return FALSE;

		if (!(hService = OpenService(hSCManager, szDriverName, SERVICE_ALL_ACCESS)))
		{
			CloseServiceHandle(hSCManager);
			return FALSE;
		}

		fStatus = DeleteService(hService);

		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);

		return fStatus;
	}
}

