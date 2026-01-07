using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Starter.PlayFabIntegration; 

public class ReferralUIHandler : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI myCodeDisplay;
    [SerializeField] private TMP_InputField friendCodeInput;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Buttons")]
    [SerializeField] private Button copyButton;
    [SerializeField] private Button redeemButton;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        // Setup listeners
        if(closeButton) closeButton.onClick.AddListener(Hide);
        if(copyButton) copyButton.onClick.AddListener(CopyCodeToClipboard);
        if(redeemButton) redeemButton.onClick.AddListener(RedeemFriendCode);

        // Ensure it starts hidden
        if(panelRoot) panelRoot.SetActive(false);
    }

    public void Show()
    {
        if(panelRoot) panelRoot.SetActive(true);
        if(statusText) statusText.text = "";
        
        LoadMyReferralCode();
    }

    public void Hide()
    {
        if(panelRoot) panelRoot.SetActive(false);
    }

    private void LoadMyReferralCode()
    {
        var mgr = PlayFabManager.Instance;
        if(mgr != null && mgr.IsLoggedIn)
        {
             // TODO: In the future, this will come from mgr.CurrentPlayerData.ReferralCode (Cloud Generated)
             // For now, we simulate the XXXX-XXXX format using the ID to test stability.
             string rawId = mgr.PlayFabId ?? "TESTID";
             myCodeDisplay.text = FormatSimulatedCode(rawId);
        }
        else
        {
            myCodeDisplay.text = "GUEST-MODE";
        }
    }

    // Temporary helper to visualize the 8-char format
    private string FormatSimulatedCode(string id)
    {
        // Makes it look like "MAU9-X7P2" (Mockup)
        string hashPart = (id.GetHashCode().ToString("X") + "XXXX").Substring(0, 4);
        string idPart = (id + "0000").Substring(0, 4);
        return $"{idPart}-{hashPart}"; 
    }

    private void CopyCodeToClipboard()
    {
        if(myCodeDisplay)
        {
            GUIUtility.systemCopyBuffer = myCodeDisplay.text;
            if(statusText) statusText.text = "Code Copied!";
        }
    }

    private void RedeemFriendCode()
    {
        string code = friendCodeInput.text.Trim();
        if(string.IsNullOrEmpty(code))
        {
             if(statusText) statusText.text = "Enter a valid code";
             return;
        }

        if(statusText) statusText.text = "Verifying Code...";
        
        Debug.Log($"[Referral] Attempting to redeem: {code}");
        
        // Real CloudScript Call
        PlayFab.PlayFabClientAPI.ExecuteCloudScript(new PlayFab.ClientModels.ExecuteCloudScriptRequest()
        {
            FunctionName = "RedeemReferral",
            FunctionParameter = new { Code = code },
            GeneratePlayStreamEvent = true
        }, 
        result => 
        {
            if(result.FunctionResult != null)
            {
                // PlayFab returns 'FunctionResult' as a generic JSON object
                string json = result.FunctionResult.ToString();
                // We check for "success": true in the JSON string or cast it
                // Simple string check for MVP:
                if (json.Contains("\"success\":true"))
                {
                    OnRedeemSuccess();
                }
                else
                {
                    string errorMsg = "Invalid Code";
                    if(json.Contains("SelfReferral")) errorMsg = "Cannot use own code";
                    if(json.Contains("AlreadyReferred")) errorMsg = "Already Redeemed";
                    
                    OnRedeemFailed(errorMsg);
                }
            }
            else
            {
                OnRedeemFailed("Unknown Error");
            }
        }, 
        error => 
        {
            OnRedeemFailed(error.ErrorMessage);
        });
    }

    private void OnRedeemSuccess()
    {
        if(statusText) 
        {
            statusText.text = "SUCCESS! Code Redeemed.";
            statusText.color = Color.green;
        }
        
        // Grant some immediate reward for the Referee?
        // Typically the Server grants it, but we can unlock local UI for feedback
        // ...
        
        Debug.Log("Referral Success!");
    }

    private void OnRedeemFailed(string error)
    {
        if(statusText) 
        {
            statusText.text = error;
            statusText.color = Color.red;
        }
        Debug.LogError($"Referral Failed: {error}");
    }

    private void SimulateSuccess()
    { 
        // Deprecated
    }
}
