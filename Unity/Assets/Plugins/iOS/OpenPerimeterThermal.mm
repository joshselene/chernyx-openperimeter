#import <Foundation/Foundation.h>

/// Returns NSProcessInfo.thermalState as an int for MobileDeviceController.
/// 0 = Nominal, 1 = Fair, 2 = Serious, 3 = Critical (iOS 11+).
extern "C" int OpenPerimeter_GetThermalState(void)
{
    if (@available(iOS 11.0, *))
    {
        return (int)NSProcessInfo.processInfo.thermalState;
    }
    return 0;
}
