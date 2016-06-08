// DriverTestHarness.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "DriverTestHarness.h"
#include <stdexcept>



	int main()
	{
		BOOL result = RegisterDriver(L"pmem", L"E:\\Code\\github\\Memory-Explorer\\MemoryExplorer\\Resources\\winpmem_x64.sys");
		result = StartDriver(L"pmem");
		result = StopDriver(L"pmem");
		result = UnregisterDriver(L"pmem");
		return 0;
	}

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
			return FALSE;

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
