using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class CombatUIGenerator
{
    public static void CreateCombatUIIfMissing(out CombatHUD hud, out CombatControlsUI controls)
    {
        hud = Object.FindAnyObjectByType<CombatHUD>(FindObjectsInactive.Include);
        controls = Object.FindAnyObjectByType<CombatControlsUI>(FindObjectsInactive.Include);

        if (hud != null && controls != null) return;

        // Crear Canvas Principal de Combate (Screen Space - Overlay)
        GameObject canvasObj = new GameObject("CombatUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Sprite simple para fondos y barras
        Sprite defaultSprite = GetDefaultUISprite();

        // ============================================
        // 1. HUD SUPERIOR (Barras de Vida)
        // ============================================
        GameObject topBar = CreateUIObject("TopHealthBarContainer", canvasObj.transform);
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = new Vector2(0f, -20f);
        topBarRect.sizeDelta = new Vector2(-80f, 100f);

        // Barra Jugador (Izquierda)
        CreateHealthBar(topBar.transform, "PlayerHealthBar", true, defaultSprite,
            out Image pFill, out TMP_Text pText, out TMP_Text pName);

        // Barra Rival (Derecha)
        CreateHealthBar(topBar.transform, "EnemyHealthBar", false, defaultSprite,
            out Image eFill, out TMP_Text eText, out TMP_Text eName);

        // Texto VS Central
        GameObject vsObj = CreateUIObject("VSText", topBar.transform);
        RectTransform vsRect = vsObj.GetComponent<RectTransform>();
        vsRect.anchorMin = new Vector2(0.5f, 0.5f);
        vsRect.anchorMax = new Vector2(0.5f, 0.5f);
        vsRect.anchoredPosition = new Vector2(0f, 0f);
        vsRect.sizeDelta = new Vector2(100f, 50f);
        TMP_Text vsText = vsObj.AddComponent<TextMeshProUGUI>();
        vsText.text = "VS";
        vsText.fontSize = 38f;
        vsText.fontStyle = FontStyles.Bold;
        vsText.alignment = TextAlignmentOptions.Center;
        vsText.color = new Color(1f, 0.85f, 0.2f);

        // ============================================
        // 2. RESULTADO DE PELEA (Victoria / Derrota)
        // ============================================
        GameObject resultPanel = CreateUIObject("ResultPanel", canvasObj.transform);
        RectTransform resRect = resultPanel.GetComponent<RectTransform>();
        resRect.anchorMin = new Vector2(0.5f, 0.5f);
        resRect.anchorMax = new Vector2(0.5f, 0.5f);
        resRect.anchoredPosition = Vector2.zero;
        resRect.sizeDelta = new Vector2(650f, 380f);

        Image resBg = resultPanel.AddComponent<Image>();
        resBg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        resBg.sprite = defaultSprite;
        resBg.type = Image.Type.Sliced;

        // Título Resultado
        GameObject resTitleObj = CreateUIObject("ResultTitle", resultPanel.transform);
        RectTransform resTitleRect = resTitleObj.GetComponent<RectTransform>();
        resTitleRect.anchorMin = new Vector2(0f, 0.65f);
        resTitleRect.anchorMax = new Vector2(1f, 1f);
        resTitleRect.anchoredPosition = Vector2.zero;
        TMP_Text resTitle = resTitleObj.AddComponent<TextMeshProUGUI>();
        resTitle.text = "¡VICTORIA!";
        resTitle.fontSize = 58f;
        resTitle.fontStyle = FontStyles.Bold;
        resTitle.alignment = TextAlignmentOptions.Center;
        resTitle.color = new Color(1f, 0.84f, 0f);

        // Subtítulo
        GameObject resSubObj = CreateUIObject("ResultSubtitle", resultPanel.transform);
        RectTransform resSubRect = resSubObj.GetComponent<RectTransform>();
        resSubRect.anchorMin = new Vector2(0f, 0.45f);
        resSubRect.anchorMax = new Vector2(1f, 0.65f);
        resSubRect.anchoredPosition = Vector2.zero;
        TMP_Text resSub = resSubObj.AddComponent<TextMeshProUGUI>();
        resSub.text = "¡Has ganado el combate!";
        resSub.fontSize = 24f;
        resSub.alignment = TextAlignmentOptions.Center;
        resSub.color = Color.white;

        // Botón Reiniciar
        Button restartBtn = CreateStyledButton(resultPanel.transform, "RestartButton",
            new Vector2(0.5f, 0.3f), new Vector2(260f, 55f), "REINICIAR PELEA", new Color(0.15f, 0.6f, 0.25f));

        // Botón Volver al Menú
        Button menuBtn = CreateStyledButton(resultPanel.transform, "MenuButton",
            new Vector2(0.5f, 0.12f), new Vector2(260f, 55f), "VOLVER AL MENÚ", new Color(0.35f, 0.4f, 0.5f));

        // Componente CombatHUD
        hud = canvasObj.AddComponent<CombatHUD>();
        SetPrivateField(hud, "playerHealthFill", pFill);
        SetPrivateField(hud, "playerHealthText", pText);
        SetPrivateField(hud, "playerNameText", pName);
        SetPrivateField(hud, "enemyHealthFill", eFill);
        SetPrivateField(hud, "enemyHealthText", eText);
        SetPrivateField(hud, "enemyNameText", eName);
        SetPrivateField(hud, "resultPanel", resultPanel);
        SetPrivateField(hud, "resultTitleText", resTitle);
        SetPrivateField(hud, "resultSubtitleText", resSub);
        SetPrivateField(hud, "restartFightButton", restartBtn);
        SetPrivateField(hud, "returnToMenuButton", menuBtn);

        resultPanel.SetActive(false);

        // ============================================
        // 3. CONTROLES MÓVILES (Joystick y Botones)
        // ============================================
        GameObject controlsContainer = CreateUIObject("ControlsContainer", canvasObj.transform);
        RectTransform contRect = controlsContainer.GetComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero;
        contRect.anchorMax = Vector2.one;
        contRect.sizeDelta = Vector2.zero;

        // Joystick Virtual (Abajo a la izquierda)
        GameObject joyBg = CreateUIObject("VirtualJoystick", controlsContainer.transform);
        RectTransform joyRect = joyBg.GetComponent<RectTransform>();
        joyRect.anchorMin = new Vector2(0f, 0f);
        joyRect.anchorMax = new Vector2(0f, 0f);
        joyRect.pivot = new Vector2(0.5f, 0.5f);
        joyRect.anchoredPosition = new Vector2(180f, 180f);
        joyRect.sizeDelta = new Vector2(180f, 180f);

        Image joyBgImg = joyBg.AddComponent<Image>();
        joyBgImg.color = new Color(1f, 1f, 1f, 0.18f);
        joyBgImg.sprite = defaultSprite;

        GameObject joyHandle = CreateUIObject("Handle", joyBg.transform);
        RectTransform handleRect = joyHandle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(80f, 80f);

        Image handleImg = joyHandle.AddComponent<Image>();
        handleImg.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        handleImg.sprite = defaultSprite;

        VirtualJoystick vJoystick = joyBg.AddComponent<VirtualJoystick>();

        // Botón Golpe (Abajo a la derecha)
        Button punchBtn = CreateStyledButton(controlsContainer.transform, "PunchButton",
            new Vector2(0.9f, 0.18f), new Vector2(120f, 120f), "GOLPE", new Color(0.85f, 0.2f, 0.2f, 0.85f));
        punchBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        // Botón Bloqueo (Abajo a la derecha, al lado del golpe)
        Button blockBtn = CreateStyledButton(controlsContainer.transform, "BlockButton",
            new Vector2(0.78f, 0.14f), new Vector2(100f, 100f), "BLOQUEO", new Color(0.2f, 0.5f, 0.9f, 0.85f));
        blockBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        // Botón de habilidad especial
        Button abilityBtn = CreateStyledButton(controlsContainer.transform, "AbilityButton",
            new Vector2(0.64f, 0.14f), new Vector2(130f, 100f), "HABILIDAD", new Color(0.75f, 0.45f, 0.12f, 0.9f));
        abilityBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        TMP_Text abilityLabel = abilityBtn.GetComponentInChildren<TMP_Text>();

        // Barra y botón de Ultimate
        Button ultimateBtn = CreateStyledButton(controlsContainer.transform, "UltimateButton",
            new Vector2(0.5f, 0.14f), new Vector2(140f, 100f), "ULT 0%", new Color(0.75f, 0.58f, 0.12f, 0.9f));
        ultimateBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        GameObject ultimateFillObject = CreateUIObject("UltimateChargeFill", ultimateBtn.transform);
        ultimateFillObject.transform.SetAsFirstSibling();
        RectTransform ultimateFillRect = ultimateFillObject.GetComponent<RectTransform>();
        ultimateFillRect.anchorMin = new Vector2(0.03f, 0.05f);
        ultimateFillRect.anchorMax = new Vector2(0.97f, 0.95f);
        ultimateFillRect.offsetMin = Vector2.zero;
        ultimateFillRect.offsetMax = Vector2.zero;
        Image ultimateFill = ultimateFillObject.AddComponent<Image>();
        ultimateFill.sprite = defaultSprite;
        ultimateFill.type = Image.Type.Filled;
        ultimateFill.fillMethod = Image.FillMethod.Horizontal;
        ultimateFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        ultimateFill.fillAmount = 0f;
        ultimateFill.color = new Color(1f, 0.84f, 0.1f, 0.45f);
        ultimateFill.raycastTarget = false;
        TMP_Text ultimateLabel = ultimateBtn.GetComponentInChildren<TMP_Text>();

        EventTrigger blockTrigger = blockBtn.gameObject.AddComponent<EventTrigger>();

        // Componente CombatControlsUI
        controls = canvasObj.AddComponent<CombatControlsUI>();
        SetPrivateField(controls, "joystick", vJoystick);
        SetPrivateField(controls, "punchButton", punchBtn);
        SetPrivateField(controls, "abilityButton", abilityBtn);
        SetPrivateField(controls, "abilityButtonLabel", abilityLabel);
        SetPrivateField(controls, "ultimateButton", ultimateBtn);
        SetPrivateField(controls, "ultimateButtonLabel", ultimateLabel);
        SetPrivateField(controls, "ultimateChargeFill", ultimateFill);
        SetPrivateField(controls, "blockButtonTrigger", blockTrigger);
    }

    private static void CreateHealthBar(Transform parent, string barName, bool isPlayer, Sprite sprite,
        out Image fillImage, out TMP_Text hpText, out TMP_Text nameText)
    {
        GameObject container = CreateUIObject(barName, parent);
        RectTransform rect = container.GetComponent<RectTransform>();

        if (isPlayer)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.42f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0.58f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        // Nombre del personaje
        GameObject nameObj = CreateUIObject("NameText", container.transform);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.6f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.anchoredPosition = Vector2.zero;
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = isPlayer ? "JUGADOR" : "RIVAL";
        nameText.fontSize = 24f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = isPlayer ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        nameText.color = Color.white;

        // Fondo de la barra
        GameObject barBg = CreateUIObject("BarBackground", container.transform);
        RectTransform bgRect = barBg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0.6f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImg = barBg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        bgImg.sprite = sprite;

        // Relleno de salud
        GameObject fillObj = CreateUIObject("Fill", barBg.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-4f, -4f);
        fillRect.anchoredPosition = Vector2.zero;

        fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = sprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = isPlayer ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
        fillImage.fillAmount = 1f;
        fillImage.color = isPlayer ? new Color(0.1f, 0.85f, 0.4f) : new Color(0.9f, 0.25f, 0.2f);

        // Texto numérico de salud
        GameObject textObj = CreateUIObject("HPText", barBg.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        hpText = textObj.AddComponent<TextMeshProUGUI>();
        hpText.text = "100 / 100";
        hpText.fontSize = 18f;
        hpText.fontStyle = FontStyles.Bold;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.white;
    }

    private static Button CreateStyledButton(Transform parent, string name, Vector2 normalizedPos, Vector2 size, string label, Color bgColor)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = normalizedPos;
        rect.anchorMax = normalizedPos;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        img.sprite = GetDefaultUISprite();
        img.type = Image.Type.Sliced;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        GameObject textObj = CreateUIObject("Label", btnObj.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = Mathf.Min(size.y * 0.45f, 26f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static Sprite GetDefaultUISprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
