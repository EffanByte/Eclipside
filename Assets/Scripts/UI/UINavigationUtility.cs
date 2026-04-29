using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UINavigationUtility
{
    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Object.DontDestroyOnLoad(root);
    }

    public static void ConfigureAutomaticNavigation(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null)
            {
                continue;
            }

            Navigation navigation = selectables[i].navigation;
            navigation.mode = Navigation.Mode.Automatic;
            selectables[i].navigation = navigation;
        }
    }

    public static void FocusFirstSelectable(Transform root)
    {
        EnsureEventSystem();
        ConfigureAutomaticNavigation(root);

        if (root == null || EventSystem.current == null)
        {
            return;
        }

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null || !selectables[i].gameObject.activeInHierarchy || !selectables[i].IsInteractable())
            {
                continue;
            }

            EventSystem.current.SetSelectedGameObject(selectables[i].gameObject);
            return;
        }
    }

    public static void Focus(Selectable selectable)
    {
        EnsureEventSystem();

        if (selectable == null || EventSystem.current == null || !selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
