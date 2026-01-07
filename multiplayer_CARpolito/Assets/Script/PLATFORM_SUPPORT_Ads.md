# Unity Ads Platform Support

## Platform Support Summary

| Platform | Unity Ads Support | Status |
|----------|------------------|--------|
| **Android** | ✅ **Supported** | Full support |
| **iOS** | ✅ **Supported** | Full support |
| **Mac** | ❌ **Not Supported** | Ads disabled, testing mode available |
| **Windows** | ❌ **Not Supported** | Ads disabled, testing mode available |
| **WebGL** | ❌ **Not Supported** | Ads disabled, testing mode available |
| **Linux** | ❌ **Not Supported** | Ads disabled, testing mode available |

## Important Notes

### Unity Ads Only Works On:
- **Android** devices
- **iOS** devices

### Unity Ads Does NOT Work On:
- **Mac** (macOS desktop)
- **Windows** (Windows desktop)
- **WebGL** (Web browsers)
- **Linux** (Linux desktop)
- Any other desktop platforms

## How The Code Handles This

The `RewardedAdsButton` component now includes platform detection:

### Supported Platforms (Android/iOS)
- Ads load and display normally
- Rewards are granted after watching ads
- Full functionality available

### Unsupported Platforms (Mac/Windows/WebGL)
- Ads are automatically disabled
- Button is disabled by default
- Console warning is shown
- **Testing Mode**: Optional setting to grant rewards directly (for development/testing)

## Testing Mode for Unsupported Platforms

In the Unity Inspector, you can enable:
- **Grant Rewards On Unsupported Platforms**: When enabled, clicking the button will grant rewards directly without showing an ad (useful for testing on Mac/Windows/WebGL)

### When to Use Testing Mode:
- ✅ Testing reward logic on desktop
- ✅ Testing UI/UX on unsupported platforms
- ✅ Development and debugging

### When NOT to Use Testing Mode:
- ❌ Production builds
- ❌ When you want to test actual ad behavior
- ❌ When testing on Android/iOS devices

## Code Behavior

### On Android/iOS:
```csharp
// Normal ad flow
ShowAd() → Loads ad → Shows video → Grants reward
```

### On Mac/Windows/WebGL (Testing Mode OFF):
```csharp
// Ads disabled
ShowAd() → Warning logged → Nothing happens
IsAdReady() → Returns false
Button → Disabled
```

### On Mac/Windows/WebGL (Testing Mode ON):
```csharp
// Direct reward (for testing)
ShowAd() → Grants reward immediately → Simulates reload
IsAdReady() → Returns true (simulated)
Button → Enabled
```

## Platform Detection

The code uses Unity's platform defines:

```csharp
#if UNITY_IOS || UNITY_ANDROID
    // Supported platform
#else
    // Unsupported platform (Mac, Windows, WebGL, etc.)
#endif
```

## Recommendations

### For Production:
1. **Disable testing mode** on unsupported platforms
2. **Hide ad buttons** on unsupported platforms (or show alternative content)
3. **Test on actual devices** (Android/iOS) before release
4. **Use platform-specific UI** to show/hide ad features

### For Development:
1. **Enable testing mode** to test reward logic on desktop
2. **Test on devices** regularly to verify ad functionality
3. **Use conditional compilation** to hide ads on unsupported platforms in production

## Example: Platform-Specific UI

```csharp
void Start()
{
    var adButton = GetComponent<RewardedAdsButton>();
    
    // Hide ad button on unsupported platforms
    if (!adButton.IsPlatformSupported())
    {
        gameObject.SetActive(false);
        // Or show alternative: "Ads available on mobile devices"
    }
}
```

## Alternative Solutions

If you need ads on desktop/WebGL, consider:

1. **AdMob** - Only supports Android/iOS (same limitation)
2. **Unity Ads** - Only supports Android/iOS (current solution)
3. **Web-based ad networks** - For WebGL (e.g., Google AdSense, but requires different integration)
4. **Platform-specific implementations** - Different ad SDKs for different platforms

## Summary

- ✅ **Android & iOS**: Full Unity Ads support
- ❌ **Mac, Windows, WebGL**: No Unity Ads support
- 🔧 **Testing Mode**: Available for development on unsupported platforms
- ⚠️ **Production**: Disable testing mode, test on actual devices

---

**Last Updated**: 2024
