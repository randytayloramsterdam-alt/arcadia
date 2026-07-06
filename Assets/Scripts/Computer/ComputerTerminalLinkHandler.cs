using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ComputerTerminalLinkHandler : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text targetText;
    public Camera uiCamera;
    public ComputerTerminalController terminalController;
    public bool enableLinks = true;
    public bool enableDebugLogs = false;

    [Header("Hover Indicator")]
    public TMP_Text hoverIndicatorText;
    public RectTransform hoverIndicatorRect;
    [Tooltip("Deprecated: no longer used for positioning. Kept for inspector reference.")]
    public RectTransform hoverIndicatorLayerRect;
    public bool enableHoverIndicator = true;
    public Color normalHoverColor = new Color(0.77f, 0.78f, 0.77f, 1f);
    public Color unreadHoverColor = new Color(0.03f, 1f, 1f, 1f);
    public float hoverIndicatorXOffset = 0f;
    public float hoverIndicatorYOffset = 0f;
    public float hoverIndicatorCharacterOffset = 2f;
    public float fallbackCharacterWidth = 20f;

    private void Start()
    {
        if (hoverIndicatorText != null)
        {
            hoverIndicatorText.text = ">";
            hoverIndicatorText.gameObject.SetActive(false);
            hoverIndicatorText.raycastTarget = false;

            LayoutElement le = hoverIndicatorText.GetComponent<LayoutElement>();
            if (le == null)
                le = hoverIndicatorText.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }
    }

    private void Update()
    {
        UpdateHoverIndicator();
    }

    private void UpdateHoverIndicator()
    {
        if (!enableHoverIndicator)
        {
            HideHoverIndicator();
            return;
        }

        if (targetText == null || terminalController == null || hoverIndicatorText == null || hoverIndicatorRect == null)
        {
            HideHoverIndicator();
            return;
        }

        Vector3 mousePos = Input.mousePosition;
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(targetText, mousePos, uiCamera);

        if (linkIndex == -1)
        {
            HideHoverIndicator();
            return;
        }

        TMP_LinkInfo linkInfo = targetText.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();
        int anchorLinkIndex = FindAnchorLinkIndex(linkId, linkIndex);

        Color color = terminalController.GetHoverColorForLink(linkId, normalHoverColor, unreadHoverColor);
        hoverIndicatorText.color = color;
        hoverIndicatorText.gameObject.SetActive(true);
        UpdateHoverIndicatorPosition(linkId, anchorLinkIndex);
    }

    private int FindAnchorLinkIndex(string linkId, int currentLinkIndex)
    {
        if (string.IsNullOrEmpty(linkId))
            return currentLinkIndex;

        if (!linkId.StartsWith("MESSAGE:", StringComparison.OrdinalIgnoreCase))
            return currentLinkIndex;

        int previousIndex = currentLinkIndex - 1;
        if (previousIndex >= 0 && previousIndex < targetText.textInfo.linkCount)
        {
            var previousLink = targetText.textInfo.linkInfo[previousIndex];
            if (previousLink.GetLinkID() == linkId)
                return previousIndex;
        }

        return currentLinkIndex;
    }

    private int FindFirstVisibleCharacterIndexForLink(TMP_LinkInfo linkInfo)
    {
        int start = linkInfo.linkTextfirstCharacterIndex;
        int length = linkInfo.linkTextLength;
        int end = start + length;

        var charInfos = targetText.textInfo.characterInfo;
        for (int i = start; i < end; i++)
        {
            if (i < 0 || i >= charInfos.Length)
                continue;

            TMP_CharacterInfo ci = charInfos[i];
            if (!ci.isVisible)
                continue;

            char c = ci.character;
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                continue;

            return i;
        }

        return start;
    }

    private void UpdateHoverIndicatorPosition(string linkId, int linkIndex)
    {
        if (targetText == null || hoverIndicatorRect == null || hoverIndicatorText == null)
            return;

        var linkInfos = targetText.textInfo.linkInfo;
        if (linkIndex < 0 || linkIndex >= linkInfos.Length)
            return;

        TMP_LinkInfo anchorLinkInfo = linkInfos[linkIndex];
        int firstVisibleCharIndex = FindFirstVisibleCharacterIndexForLink(anchorLinkInfo);

        if (firstVisibleCharIndex < 0 || firstVisibleCharIndex >= targetText.textInfo.characterInfo.Length)
            return;

        TMP_CharacterInfo firstVisibleChar = targetText.textInfo.characterInfo[firstVisibleCharIndex];

        float charWidth = fallbackCharacterWidth;
        float xAdvance = firstVisibleChar.xAdvance;
        float origin = firstVisibleChar.origin;
        if (xAdvance > origin)
        {
            charWidth = xAdvance - origin;
        }

        Vector3 localPosInText = firstVisibleChar.topLeft;
        localPosInText.x -= charWidth * hoverIndicatorCharacterOffset;
        localPosInText.y += hoverIndicatorYOffset;

        RectTransform targetRect = targetText.rectTransform;
        RectTransform indicatorParentRect = hoverIndicatorRect.parent as RectTransform;

        Vector3 worldPos = targetRect.TransformPoint(localPosInText);
        Vector3 parentLocalPos = indicatorParentRect.InverseTransformPoint(worldPos);

        hoverIndicatorRect.localPosition = parentLocalPos + new Vector3(hoverIndicatorXOffset, 0f, 0f);
    }

    private void HideHoverIndicator()
    {
        if (hoverIndicatorText != null)
            hoverIndicatorText.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableLinks)
            return;

        if (targetText == null || terminalController == null)
            return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(targetText, eventData.position, uiCamera);
        if (linkIndex == -1)
            return;

        TMP_LinkInfo linkInfo = targetText.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();

        if (enableDebugLogs)
            Debug.Log($"[ComputerTerminalLinkHandler] Clicked link: {linkId}");

        terminalController.HandleTerminalLinkClicked(linkId);
    }
}