using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Starter.PlayFabIntegration
{
    public static class PlayFabUIGenerator
    {
        public static void GenerateAuthUI(PlayFabAuthUI target)
        {
            if (target == null) return;

            // 1. Find or Create Canvas
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Create Main Auth Panel (The Cover)
            GameObject authPanelObj = CreatePanel(canvas.transform, "AuthPanel_Cover", new Color(0, 0, 0, 0.95f));
            SetFullStretch(authPanelObj.GetComponent<RectTransform>());

            // Assign to target
            SetPrivateField(target, "authPanel", authPanelObj);

            // 3. Create Login Panel
            GameObject loginPanelObj = CreatePanel(authPanelObj.transform, "LoginPanel", new Color(0, 0, 0, 0)); // Transparent container
            SetFullStretch(loginPanelObj.GetComponent<RectTransform>());
            SetPrivateField(target, "loginPanel", loginPanelObj);

            // Title
            CreateText(loginPanelObj.transform, "Title", "BIENVENIDO SOLDADO", 60, new Vector2(0, 300));

            // Inputs
            TMP_InputField emailInput = CreateInputField(loginPanelObj.transform, "EmailInput", "Correo Electrónico", new Vector2(0, 100));
            SetPrivateField(target, "loginEmailInput", emailInput);

            TMP_InputField passInput = CreateInputField(loginPanelObj.transform, "PassInput", "Contraseña", new Vector2(0, 0));
            passInput.contentType = TMP_InputField.ContentType.Password;
            SetPrivateField(target, "loginPasswordInput", passInput);

            // Buttons
            Button loginBtn = CreateButton(loginPanelObj.transform, "LoginButton", "INICIAR SESIÓN", new Vector2(0, -100), Color.green);
            SetPrivateField(target, "loginButton", loginBtn);

            Button guestBtn = CreateButton(loginPanelObj.transform, "GuestButton", "JUGAR COMO INVITADO\n<size=20>(Sin premios ni ranking)</size>", new Vector2(0, -250), Color.gray);
            RectTransform guestRect = guestBtn.GetComponent<RectTransform>();
            guestRect.sizeDelta = new Vector2(400, 100); // Taller for 2 lines
            SetPrivateField(target, "guestLoginButton", guestBtn);

            Button registerLinkBtn = CreateButton(loginPanelObj.transform, "RegisterLinkButton", "CREAR CUENTA NUEVA", new Vector2(0, -400), Color.clear);
            registerLinkBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.cyan;
            SetPrivateField(target, "goToRegisterButton", registerLinkBtn);

            // Error Text
            TextMeshProUGUI loginError = CreateText(loginPanelObj.transform, "LoginError", "", 30, new Vector2(0, -180));
            loginError.color = Color.red;
            loginError.gameObject.SetActive(false);
            SetPrivateField(target, "loginErrorText", loginError);


            // 4. Create Register Panel (Hidden by default)
            GameObject registerPanelObj = CreatePanel(authPanelObj.transform, "RegisterPanel", new Color(0, 0, 0, 0));
            SetFullStretch(registerPanelObj.GetComponent<RectTransform>());
            registerPanelObj.SetActive(false);
            SetPrivateField(target, "registerPanel", registerPanelObj);

            CreateText(registerPanelObj.transform, "Title", "NUEVA CUENTA", 60, new Vector2(0, 350));

            TMP_InputField regUser = CreateInputField(registerPanelObj.transform, "UserInput", "Nombre de Usuario", new Vector2(0, 200));
            SetPrivateField(target, "registerDisplayNameInput", regUser);

            TMP_InputField regEmail = CreateInputField(registerPanelObj.transform, "EmailInput", "Correo Electrónico", new Vector2(0, 100));
            SetPrivateField(target, "registerEmailInput", regEmail);

            TMP_InputField regPass = CreateInputField(registerPanelObj.transform, "PassInput", "Contraseña", new Vector2(0, 0));
            regPass.contentType = TMP_InputField.ContentType.Password;
            SetPrivateField(target, "registerPasswordInput", regPass);

            TMP_InputField regPassConf = CreateInputField(registerPanelObj.transform, "PassConfInput", "Confirmar Contraseña", new Vector2(0, -100));
            regPassConf.contentType = TMP_InputField.ContentType.Password;
            SetPrivateField(target, "registerConfirmPasswordInput", regPassConf);

            TMP_InputField refCode = CreateInputField(registerPanelObj.transform, "RefCodeInput", "Código de Referido (Opcional)", new Vector2(0, -200));
            SetPrivateField(target, "referralCodeInput", refCode);

            Button regBtn = CreateButton(registerPanelObj.transform, "RegisterButton", "REGISTRARSE", new Vector2(0, -300), Color.blue);
            SetPrivateField(target, "registerButton", regBtn);

            Button backBtn = CreateButton(registerPanelObj.transform, "BackLoginButton", "Volver al Login", new Vector2(0, -400), Color.clear);
            backBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
            SetPrivateField(target, "goToLoginButton", backBtn);
            
            TextMeshProUGUI regError = CreateText(registerPanelObj.transform, "RegisterError", "", 30, new Vector2(0, -260));
            regError.color = Color.red;
            regError.gameObject.SetActive(false);
            SetPrivateField(target, "registerErrorText", regError);

            // 5. Loading Panel
            GameObject loadingPanelObj = CreatePanel(authPanelObj.transform, "LoadingPanel", new Color(0, 0, 0, 0.9f));
            SetFullStretch(loadingPanelObj.GetComponent<RectTransform>());
            loadingPanelObj.SetActive(false);
            SetPrivateField(target, "loadingPanel", loadingPanelObj);

            TextMeshProUGUI loadingTxt = CreateText(loadingPanelObj.transform, "LoadingText", "Cargando...", 50, Vector2.zero);
            SetPrivateField(target, "loadingText", loadingTxt);

            Debug.Log("[PlayFabUIGenerator] Cover UI generated successfully!");
        }

        // --- Helpers ---

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private static TMP_InputField CreateInputField(Transform parent, string name, string placeholderText, Vector2 pos)
        {
            // Simple Input Field Structure: Root(Image) -> TextArea (RectMask) -> Text + Placeholder
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Image bg = root.AddComponent<Image>();
            bg.color = Color.white;
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = pos;

            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(root.transform, false);
            RectTransform areaRect = textArea.AddComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10, 5);
            areaRect.offsetMax = new Vector2(-10, -5);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 24;
            textComp.color = Color.black;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeComp = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeComp.fontSize = 24;
            placeComp.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeComp.text = placeholderText;
            placeComp.fontStyle = FontStyles.Italic;
            RectTransform placeRect = placeholderObj.GetComponent<RectTransform>();
            placeRect.anchorMin = Vector2.zero;
            placeRect.anchorMax = Vector2.one;
            placeRect.offsetMin = Vector2.zero;
            placeRect.offsetMax = Vector2.zero;

            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = textComp;
            input.placeholder = placeComp;
            
            return input;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = pos;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = Color.black;
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 100);
            rect.anchoredPosition = pos;
            return tmp;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}
