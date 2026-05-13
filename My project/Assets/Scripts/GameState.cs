using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public static class GameState
{
    public static bool IsUIOpen()
    {
        // Check VS scene variable
        try
        {
            if ((bool)Variables.Scene(SceneManager.GetActiveScene()).Get("isOpen"))
                return true;
        }
        catch { }

        // Check UI Toolkit panels
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsInventoryOpen()) return true;
            if (UIManager.Instance.IsCraftingOpen())  return true;
        }

        return false;
    }
}
