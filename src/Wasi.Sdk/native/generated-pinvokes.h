// GENERATED FILE, DO NOT MODIFY

int SystemNative_Access (int,int);
int SystemNative_AlignedAlloc (int,int);
void SystemNative_AlignedFree (int);
int SystemNative_AlignedRealloc (int,int,int);
int SystemNative_Calloc (int,int);
int SystemNative_CanGetHiddenFlag ();
int SystemNative_ChDir (int);
int SystemNative_ChMod (int,int);
int SystemNative_Close (int);
int SystemNative_CloseDir (int);
int SystemNative_ConvertErrorPalToPlatform (int);
int SystemNative_ConvertErrorPlatformToPal (int);
int SystemNative_CopyFile (int,int,int64_t);
int SystemNative_Dup (int);
int SystemNative_FAllocate (int,int64_t,int64_t);
int SystemNative_FChflags (int,int);
int SystemNative_FChMod (int,int);
int SystemNative_FLock (int,int);
void SystemNative_Free (int);
void SystemNative_FreeEnviron (int);
int SystemNative_FStat (int,int);
int SystemNative_FSync (int);
int SystemNative_FTruncate (int,int64_t);
int SystemNative_FUTimens (int,int);
int SystemNative_GetCpuUtilization (int);
int SystemNative_GetCryptographicallySecureRandomBytes (int,int);
int SystemNative_GetCwd (int,int);
int SystemNative_GetDefaultSearchOrderPseudoHandle ();
int SystemNative_GetEnv (int);
int SystemNative_GetEnviron ();
int SystemNative_GetErrNo ();
int SystemNative_GetFileSystemType (int);
void SystemNative_GetNonCryptographicallySecureRandomBytes (int,int);
int SystemNative_GetReadDirRBufferSize ();
int64_t SystemNative_GetSystemTimeAsTicks ();
uint64_t SystemNative_GetTimestamp ();
int SystemNative_LChflags (int,int);
int SystemNative_LChflagsCanSetHiddenFlag ();
int SystemNative_Link (int,int);
int SystemNative_LockFileRegion (int,int64_t,int64_t,int);
void SystemNative_Log (int,int);
void SystemNative_LogError (int,int);
void SystemNative_LowLevelMonitor_Acquire (int);
int SystemNative_LowLevelMonitor_Create ();
void SystemNative_LowLevelMonitor_Destroy (int);
void SystemNative_LowLevelMonitor_Release (int);
void SystemNative_LowLevelMonitor_Signal_Release (int);
int SystemNative_LowLevelMonitor_TimedWait (int,int);
void SystemNative_LowLevelMonitor_Wait (int);
int64_t SystemNative_LSeek (int,int64_t,int);
int SystemNative_LStat (int,int);
int SystemNative_Malloc (int);
int SystemNative_MkDir (int,int);
int SystemNative_MksTemps (int,int);
int SystemNative_Open (int,int,int);
int SystemNative_OpenDir (int);
int SystemNative_PosixFAdvise (int,int64_t,int64_t,int);
int SystemNative_PRead (int,int,int,int64_t);
int64_t SystemNative_PReadV (int,int,int,int64_t);
int SystemNative_PWrite (int,int,int,int64_t);
int64_t SystemNative_PWriteV (int,int,int,int64_t);
int SystemNative_Read (int,int,int);
int SystemNative_ReadDirR (int,int,int,int);
int SystemNative_ReadLink (int,int,int);
int SystemNative_Realloc (int,int);
int SystemNative_Rename (int,int);
int SystemNative_RmDir (int);
void SystemNative_SetErrNo (int);
int SystemNative_Stat (int,int);
int SystemNative_StrErrorR (int,int,int);
int SystemNative_SymLink (int,int);
int64_t SystemNative_SysConf (int);
void SystemNative_SysLog (int,int,int);
int SystemNative_Unlink (int);
int SystemNative_UTimensat (int,int);
int SystemNative_Write (int,int,int);
static PinvokeImport libSystem_Native_imports [] = {
{"SystemNative_Access", SystemNative_Access}, // System.Private.CoreLib
{"SystemNative_AlignedAlloc", SystemNative_AlignedAlloc}, // System.Private.CoreLib
{"SystemNative_AlignedFree", SystemNative_AlignedFree}, // System.Private.CoreLib
{"SystemNative_AlignedRealloc", SystemNative_AlignedRealloc}, // System.Private.CoreLib
{"SystemNative_Calloc", SystemNative_Calloc}, // System.Private.CoreLib
{"SystemNative_CanGetHiddenFlag", SystemNative_CanGetHiddenFlag}, // System.Private.CoreLib
{"SystemNative_ChDir", SystemNative_ChDir}, // System.Private.CoreLib
{"SystemNative_ChMod", SystemNative_ChMod}, // System.Private.CoreLib
{"SystemNative_Close", SystemNative_Close}, // System.Private.CoreLib
{"SystemNative_CloseDir", SystemNative_CloseDir}, // System.Private.CoreLib
{"SystemNative_ConvertErrorPalToPlatform", SystemNative_ConvertErrorPalToPlatform}, // System.Console, System.Private.CoreLib
{"SystemNative_ConvertErrorPlatformToPal", SystemNative_ConvertErrorPlatformToPal}, // System.Console, System.Private.CoreLib
{"SystemNative_CopyFile", SystemNative_CopyFile}, // System.Private.CoreLib
{"SystemNative_Dup", SystemNative_Dup}, // System.Console
{"SystemNative_FAllocate", SystemNative_FAllocate}, // System.Private.CoreLib
{"SystemNative_FChflags", SystemNative_FChflags}, // System.Private.CoreLib
{"SystemNative_FChMod", SystemNative_FChMod}, // System.Private.CoreLib
{"SystemNative_FLock", SystemNative_FLock}, // System.Private.CoreLib
{"SystemNative_Free", SystemNative_Free}, // System.Private.CoreLib
{"SystemNative_FreeEnviron", SystemNative_FreeEnviron}, // System.Private.CoreLib
{"SystemNative_FStat", SystemNative_FStat}, // System.Private.CoreLib
{"SystemNative_FSync", SystemNative_FSync}, // System.Private.CoreLib
{"SystemNative_FTruncate", SystemNative_FTruncate}, // System.Private.CoreLib
{"SystemNative_FUTimens", SystemNative_FUTimens}, // System.Private.CoreLib
{"SystemNative_GetCpuUtilization", SystemNative_GetCpuUtilization}, // System.Private.CoreLib
{"SystemNative_GetCryptographicallySecureRandomBytes", SystemNative_GetCryptographicallySecureRandomBytes}, // System.Private.CoreLib
{"SystemNative_GetCwd", SystemNative_GetCwd}, // System.Private.CoreLib
{"SystemNative_GetDefaultSearchOrderPseudoHandle", SystemNative_GetDefaultSearchOrderPseudoHandle}, // System.Private.CoreLib
{"SystemNative_GetEnv", SystemNative_GetEnv}, // System.Private.CoreLib
{"SystemNative_GetEnviron", SystemNative_GetEnviron}, // System.Private.CoreLib
{"SystemNative_GetErrNo", SystemNative_GetErrNo}, // System.Private.CoreLib
{"SystemNative_GetFileSystemType", SystemNative_GetFileSystemType}, // System.Private.CoreLib
{"SystemNative_GetNonCryptographicallySecureRandomBytes", SystemNative_GetNonCryptographicallySecureRandomBytes}, // System.Private.CoreLib
{"SystemNative_GetReadDirRBufferSize", SystemNative_GetReadDirRBufferSize}, // System.Private.CoreLib
{"SystemNative_GetSystemTimeAsTicks", SystemNative_GetSystemTimeAsTicks}, // System.Private.CoreLib
{"SystemNative_GetTimestamp", SystemNative_GetTimestamp}, // System.Private.CoreLib
{"SystemNative_LChflags", SystemNative_LChflags}, // System.Private.CoreLib
{"SystemNative_LChflagsCanSetHiddenFlag", SystemNative_LChflagsCanSetHiddenFlag}, // System.Private.CoreLib
{"SystemNative_Link", SystemNative_Link}, // System.Private.CoreLib
{"SystemNative_LockFileRegion", SystemNative_LockFileRegion}, // System.Private.CoreLib
{"SystemNative_Log", SystemNative_Log}, // System.Private.CoreLib
{"SystemNative_LogError", SystemNative_LogError}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_Acquire", SystemNative_LowLevelMonitor_Acquire}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_Create", SystemNative_LowLevelMonitor_Create}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_Destroy", SystemNative_LowLevelMonitor_Destroy}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_Release", SystemNative_LowLevelMonitor_Release}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_Signal_Release", SystemNative_LowLevelMonitor_Signal_Release}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_TimedWait", SystemNative_LowLevelMonitor_TimedWait}, // System.Private.CoreLib
{"SystemNative_LowLevelMonitor_Wait", SystemNative_LowLevelMonitor_Wait}, // System.Private.CoreLib
{"SystemNative_LSeek", SystemNative_LSeek}, // System.Private.CoreLib
{"SystemNative_LStat", SystemNative_LStat}, // System.Private.CoreLib
{"SystemNative_Malloc", SystemNative_Malloc}, // System.Private.CoreLib
{"SystemNative_MkDir", SystemNative_MkDir}, // System.Private.CoreLib
{"SystemNative_MksTemps", SystemNative_MksTemps}, // System.Private.CoreLib
{"SystemNative_Open", SystemNative_Open}, // System.Private.CoreLib
{"SystemNative_OpenDir", SystemNative_OpenDir}, // System.Private.CoreLib
{"SystemNative_PosixFAdvise", SystemNative_PosixFAdvise}, // System.Private.CoreLib
{"SystemNative_PRead", SystemNative_PRead}, // System.Private.CoreLib
{"SystemNative_PReadV", SystemNative_PReadV}, // System.Private.CoreLib
{"SystemNative_PWrite", SystemNative_PWrite}, // System.Private.CoreLib
{"SystemNative_PWriteV", SystemNative_PWriteV}, // System.Private.CoreLib
{"SystemNative_Read", SystemNative_Read}, // System.Private.CoreLib
{"SystemNative_ReadDirR", SystemNative_ReadDirR}, // System.Private.CoreLib
{"SystemNative_ReadLink", SystemNative_ReadLink}, // System.Private.CoreLib
{"SystemNative_Realloc", SystemNative_Realloc}, // System.Private.CoreLib
{"SystemNative_Rename", SystemNative_Rename}, // System.Private.CoreLib
{"SystemNative_RmDir", SystemNative_RmDir}, // System.Private.CoreLib
{"SystemNative_SetErrNo", SystemNative_SetErrNo}, // System.Private.CoreLib
{"SystemNative_Stat", SystemNative_Stat}, // System.Private.CoreLib
{"SystemNative_StrErrorR", SystemNative_StrErrorR}, // System.Console, System.Private.CoreLib
{"SystemNative_SymLink", SystemNative_SymLink}, // System.Private.CoreLib
{"SystemNative_SysConf", SystemNative_SysConf}, // System.Private.CoreLib
{"SystemNative_SysLog", SystemNative_SysLog}, // System.Private.CoreLib
{"SystemNative_Unlink", SystemNative_Unlink}, // System.Private.CoreLib
{"SystemNative_UTimensat", SystemNative_UTimensat}, // System.Private.CoreLib
{"SystemNative_Write", SystemNative_Write}, // System.Console, System.Private.CoreLib
{NULL, NULL}
};
static void *pinvoke_tables[] = { libSystem_Native_imports,};
static char *pinvoke_names[] = { "libSystem.Native",};
