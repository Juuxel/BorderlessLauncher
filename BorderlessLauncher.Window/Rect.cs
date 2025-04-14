// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Windows.Win32.Foundation;

namespace BorderlessLauncher.Window;

public record Rect(int Left, int Top, int Right, int Bottom)
{
    internal Rect(RECT rect) : this(rect.left, rect.top, rect.right, rect.bottom) { }
}
