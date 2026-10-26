import Foundation
import CoreGraphics

for _ in 0..<30 {
    let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    if let window = windows.first(where: {
        $0[kCGWindowOwnerName as String] as? String == "EarthBackground" &&
        $0[kCGWindowLayer as String] as? Int == 0
    }) {
        print(window)
        exit(0)
    }
    Thread.sleep(forTimeInterval: 1)
}
fputs("EarthBackground did not display a window within 30 seconds.\n", stderr)
exit(1)
