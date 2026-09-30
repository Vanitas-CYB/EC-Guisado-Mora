using System;
using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Habilita el proveedor OpenXR dentro de XR Plug-in Management para la plataforma Standalone (Windows).
/// Equivale a marcar manualmente Project Settings > XR Plug-in Management > PC > OpenXR.
///
/// Menu: EC_XR / 2. Habilitar OpenXR en XR Plug-in Management
/// CLI : Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod ConfigurarXRPlugins.HabilitarOpenXR
/// </summary>
public static class ConfigurarXRPlugins
{
    private const string LoaderOpenXR = "UnityEngine.XR.OpenXR.OpenXRLoader";
    private const string RutaContenedor = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
    private const string RutaCarpetaXR = "Assets/XR/Settings";

    [MenuItem("EC_XR/2. Habilitar OpenXR en XR Plug-in Management")]
    public static void HabilitarOpenXR()
    {
        try
        {
            XRGeneralSettingsPerBuildTarget contenedor = ObtenerOCrearContenedor();
            if (contenedor == null)
            {
                Debug.LogError("[EC_XR] No se pudo obtener ni crear XRGeneralSettingsPerBuildTarget.");
                return;
            }

            if (!contenedor.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))
            {
                contenedor.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }

            if (!contenedor.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone))
            {
                contenedor.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }

            XRGeneralSettings settings = contenedor.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            XRManagerSettings manager = contenedor.ManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);

            if (settings == null || manager == null)
            {
                Debug.LogError("[EC_XR] No se pudieron crear los ajustes XR para Standalone.");
                return;
            }

            settings.InitManagerOnStart = true;

            bool asignado = XRPackageMetadataStore.AssignLoader(manager, LoaderOpenXR, BuildTargetGroup.Standalone);

            EditorUtility.SetDirty(contenedor);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();

            if (asignado && OpenXRHabilitado())
            {
                Debug.Log("[EC_XR] OpenXR habilitado correctamente en XR Plug-in Management (Standalone)."
                    + " Cargadores activos: " + manager.activeLoaders.Count);
            }
            else
            {
                Debug.LogWarning("[EC_XR] No se pudo confirmar la asignacion automatica del loader de OpenXR. "
                    + "Habilitalo a mano en Project Settings > XR Plug-in Management > PC > OpenXR.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[EC_XR] Error habilitando OpenXR: " + e);
        }
    }

    /// <summary>Devuelve el contenedor de ajustes XR, creandolo si no existe.</summary>
    private static XRGeneralSettingsPerBuildTarget ObtenerOCrearContenedor()
    {
        if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget contenedor)
            && contenedor != null)
        {
            return contenedor;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            XRGeneralSettingsPerBuildTarget encontrado = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(ruta);
            if (encontrado != null)
            {
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, encontrado, true);
                return encontrado;
            }
        }

        Directory.CreateDirectory(RutaCarpetaXR);
        AssetDatabase.Refresh();

        contenedor = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        AssetDatabase.CreateAsset(contenedor, RutaContenedor);
        AssetDatabase.SaveAssets();
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, contenedor, true);

        return contenedor;
    }

    /// <summary>Informa si el loader de OpenXR esta activo para Standalone.</summary>
    public static bool OpenXRHabilitado()
    {
        try
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings == null || settings.Manager == null || settings.Manager.activeLoaders == null)
            {
                return false;
            }

            foreach (XRLoader loader in settings.Manager.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == LoaderOpenXR)
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Se informa como no habilitado.
        }

        return false;
    }
}
