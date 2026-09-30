using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ejecuta en orden todos los pasos del proyecto:
///   1. Construir la escena XR
///   2. Habilitar OpenXR en XR Plug-in Management
///   3. Verificar la escena contra la rubrica
///
/// Menu: EC_XR / 9. Ejecutar todo (escena + XR + verificacion)
/// CLI : Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod EjecutarTodo.Todo
/// </summary>
public static class EjecutarTodo
{
    [MenuItem("EC_XR/9. Ejecutar todo (escena + XR + verificacion)")]
    public static void Todo()
    {
        Ejecutar("Construir escena", ConstruirEscenaXR.Construir);
        Ejecutar("Habilitar OpenXR", ConfigurarXRPlugins.HabilitarOpenXR);
        Ejecutar("Verificar escena", VerificarEscenaXR.Verificar);
        Ejecutar("Diagnostico renderers", DiagnosticoEscena.Volcar);
        Ejecutar("Guardar assets", () => AssetDatabase.SaveAssets());

        Debug.Log("[EC_XR] PROCESO COMPLETO FINALIZADO");
    }

    /// <summary>Igual que Todo pero ademas genera las capturas (requiere GPU, no usar -nographics).</summary>
    public static void TodoConCapturas()
    {
        Todo();
        Ejecutar("Capturar evidencias", CapturarEvidencias.Capturar);
        Debug.Log("[EC_XR] PROCESO COMPLETO + CAPTURAS FINALIZADO");
    }

    private static void Ejecutar(string nombre, Action accion)
    {
        try
        {
            Debug.Log("[EC_XR] >> " + nombre);
            accion();
            Debug.Log("[EC_XR] << OK: " + nombre);
        }
        catch (Exception e)
        {
            Debug.LogError("[EC_XR] << FALLO en '" + nombre + "': " + e);
        }
    }
}
