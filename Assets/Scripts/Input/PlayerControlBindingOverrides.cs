using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PlayerControlBindingOverrides
{
    public static void ApplySavedOverrides(PlayerControls controls)
    {
        if (controls == null)
        {
            return;
        }

        string json = SaveManager.Settings?.controls?.binding_overrides_json;
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            controls.asset.LoadBindingOverridesFromJson(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Input] Failed to load binding overrides. {ex.Message}");
        }
    }

    public static void SaveOverrides(PlayerControls controls)
    {
        if (controls == null || controls.asset == null)
        {
            return;
        }

        try
        {
            string json = controls.asset.SaveBindingOverridesAsJson();
            SaveFile_Settings settings = SaveManager.Settings;
            if (settings.controls == null)
            {
                settings.controls = new ControlSettings();
            }

            settings.controls.binding_overrides_json = json;
            SaveManager.SaveSettings();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Input] Failed to save binding overrides. {ex.Message}");
        }
    }

    public static void ClearOverrides(PlayerControls controls)
    {
        if (controls == null || controls.asset == null)
        {
            return;
        }

        controls.asset.RemoveAllBindingOverrides();

        SaveFile_Settings settings = SaveManager.Settings;
        if (settings.controls == null)
        {
            settings.controls = new ControlSettings();
        }

        settings.controls.binding_overrides_json = string.Empty;
        SaveManager.SaveSettings();
    }
}
