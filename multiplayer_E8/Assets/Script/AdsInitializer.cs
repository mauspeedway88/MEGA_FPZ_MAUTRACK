using UnityEngine;
using GoogleMobileAds.Api;

public class AdsInitializer : MonoBehaviour
{
  [Header("AdMob App IDs")]
  [SerializeField] string _androidAppId = "ca-app-pub-6016513053121401~5703639775";
  [SerializeField] string _iOSAppId = "ca-app-pub-6016513053121401~5703639775";
  [SerializeField] bool _testMode = true;

  private string _appId;

  void Awake()
  {
    InitializeAds();
  }

  public void InitializeAds()
  {
    #if UNITY_IOS
    _appId = _iOSAppId;
    #elif UNITY_ANDROID
    _appId = _androidAppId;
    #elif UNITY_EDITOR
    _appId = _androidAppId; // For testing in editor
    #endif

    // Initialize Google Mobile Ads SDK
    MobileAds.Initialize((InitializationStatus initStatus) =>
    {
      if (initStatus == null)
      {
        Debug.LogError("Google Mobile Ads initialization failed.");
        return;
      }

      Debug.Log("Google Mobile Ads initialization complete.");

      // Configure test mode if needed
      // Note: Test ads are automatically shown in development builds
      // For production, disable test mode and use Ad Inspector for testing
      if (_testMode)
  {
        Debug.Log("AdMob test mode enabled. Test ads will be shown automatically.");
      }
    });
  }
}
