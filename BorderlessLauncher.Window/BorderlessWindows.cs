// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Windows.Win32.UI.WindowsAndMessaging;

namespace BorderlessLauncher.Window;

public static class BorderlessWindows
{
    internal const WINDOW_STYLE BorderlessStyle = WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPCHILDREN;
}
