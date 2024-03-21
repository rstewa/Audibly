// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/21/2024

using System.Runtime.InteropServices;

namespace Audibly.App.Helpers;

internal class NativeHelper
{
    public const int ERROR_SUCCESS = 0;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;
    public const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("api-ms-win-appmodel-runtime-l1-1-1", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U4)]
    internal static extern uint GetCurrentPackageId(ref int pBufferLength, out byte pBuffer);

    public static bool IsAppPackaged
    {
        get
        {
            var bufferSize = 0;
            byte byteBuffer = 0;
            var lastError = GetCurrentPackageId(ref bufferSize, out byteBuffer);
            var isPackaged = true;

            if (lastError == APPMODEL_ERROR_NO_PACKAGE) isPackaged = false;
            return isPackaged;
        }
    }
}