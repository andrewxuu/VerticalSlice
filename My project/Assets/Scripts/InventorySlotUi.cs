using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI countText;

    void Awake()
    {
        Clear();
    }

    public void SetItem(ItemData item, int count)
    {
        if (iconImage != null)
        {
            iconImage.sprite  = item.icon;
            iconImage.enabled = item.icon != null;
        }
        if (countText != null)
            countText.text = count > 1 ? count.ToString() : "";
    }

    public void Clear()
    {
        if (iconImage  != null) { iconImage.enabled = false; iconImage.sprite = null; }
        if (countText  != null) countText.text = "";
    }
}