using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Volca a Logs/EC_XR_renderers.log todos los MeshRenderer de la escena con su posicion,
/// material y shader, para detectar materiales roto o scripts perdidos.
///
/// Menu: EC_XR / 5. Diagnostico de renderers
/// CLI : Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod DiagnosticoEscena.Volcar
/// </summary>
public static class DiagnosticoEscena
{
    [MenuItem("EC_XR/5. Diagnostico de renderers")]
    public static void Volcar()
    {
        var informe = new StringBuilder();
        informe.AppendLine("=== DIAGNOSTICO DE RENDERERS ===");

        int error = 0;

        foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            Material material = renderer.sharedMaterial;
            string nombreMaterial = material != null ? material.name : "SIN MATERIAL";
            string nombreShader = (material != null && material.shader != null) ? material.shader.name : "SIN SHADER";

            bool roto = material == null
                || material.shader == null
                || nombreShader.Contains("InternalError")
                || nombreShader.Contains("error");

            if (roto)
            {
                error++;
            }

            informe.AppendLine(string.Format("{0} | pos {1} | mat: {2} | shader: {3}",
                renderer.name, renderer.transform.position.ToString("F2"), nombreMaterial, nombreShader));
        }

        // Tambien se revisan los componentes de UI.
        foreach (UnityEngine.UI.Graphic grafico in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
        {
            Material material = grafico.material;
            informe.AppendLine(string.Format("[UI] {0} | pos {1} | mat: {2} | shader: {3}",
                grafico.name, grafico.transform.position.ToString("F2"),
                material != null ? material.name : "SIN MATERIAL",
                (material != null && material.shader != null) ? material.shader.name : "SIN SHADER"));
        }

        informe.AppendLine("Renderers con material roto: " + error);

        string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "EC_XR_renderers.log");
        File.WriteAllText(ruta, informe.ToString(), Encoding.UTF8);
        Debug.Log("[EC_XR] Diagnostico de renderers: " + error + " material(es) roto(s). Ver " + ruta);
    }
}
