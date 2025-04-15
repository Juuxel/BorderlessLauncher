// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace BorderlessLauncher.Interop;

public static class InteropUtil
{
    public unsafe delegate T CharPtrFunc<T>(char* ptr);

    public static T WithCharPtr<T>(string str, CharPtrFunc<T> func)
    {
        unsafe
        {
            fixed (char* ptr = str)
            {
                return func.Invoke(ptr);
            }
        }
    }
}
