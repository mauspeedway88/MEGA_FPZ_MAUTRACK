# How to Install Google Mobile Ads (AdMob) for Unity

The Google Mobile Ads package cannot be added directly to `manifest.json`. You need to install it using one of these methods:

## Method 1: OpenUPM (Recommended)

### Step 1: Add OpenUPM Registry
1. Open Unity Editor
2. Go to **Edit → Project Settings → Package Manager**
3. Under **Scoped Registries**, click the **+** button
4. Add the following:
   - **Name**: `OpenUPM`
   - **URL**: `https://package.openupm.com`
   - **Scope(s)**: `com.google`
5. Click **Save**

### Step 2: Install Google Mobile Ads Package
1. Open **Window → Package Manager**
2. In the top-left dropdown, select **My Registries** (instead of "In Project")
3. Find **Google Mobile Ads for Unity** in the list
4. Click **Install**

## Method 2: Manual Installation (.unitypackage)

### Step 1: Download the Package
1. Go to: https://github.com/googleads/googleads-mobile-unity/releases
2. Download the latest `.unitypackage` file (e.g., `GoogleMobileAdsPlugin-v10.6.0.unitypackage`)

### Step 2: Import into Unity
1. In Unity Editor, go to **Assets → Import Package → Custom Package**
2. Select the downloaded `.unitypackage` file
3. Click **Import** and select all items

## Method 3: OpenUPM CLI (Advanced)

If you have OpenUPM CLI installed:
```bash
cd "path/to/your/project"
openupm add com.google.ads.mobile
```

## After Installation

### Step 1: Set AdMob App ID in Unity Editor
1. Go to **Assets → Google Mobile Ads → Settings**
2. Enter your App ID:
   - **Android App ID**: `ca-app-pub-6016513053121401~5703639775`
   - **iOS App ID**: `ca-app-pub-6016513053121401~5703639775`

### Step 2: Resolve Android Dependencies (Android only)
1. Go to **Assets → External Dependency Manager → Android Resolver → Force Resolve**
2. Wait for dependencies to resolve

### Step 3: Verify Installation
- Check that `GoogleMobileAds` namespace is available
- Your `AdsInitializer.cs` and `RewardedAdsButton.cs` should compile without errors

## Troubleshooting

### Package Not Found Error
- Make sure you've added the OpenUPM registry (Method 1)
- Or use manual installation (Method 2)

### Compilation Errors
- Make sure the package is fully imported
- Check that all dependencies are resolved
- Restart Unity Editor if needed

### Android Build Errors
- Run **Assets → External Dependency Manager → Android Resolver → Force Resolve**
- Make sure your Android SDK is properly configured

## Current Configuration

Your code is already configured with:
- **App ID**: `ca-app-pub-6016513053121401~5703639775`
- **Rewarded Ad Unit ID**: `ca-app-pub-6016513053121401/1716293301`
- **Interstitial Ad Unit ID**: `ca-app-pub-6016513053121401/4108305313` (if you need it later)

Once the package is installed, everything should work automatically!

---

**Note**: The package name in code is `com.google.ads.mobile`, but it must be installed via OpenUPM or .unitypackage, not directly in manifest.json.
