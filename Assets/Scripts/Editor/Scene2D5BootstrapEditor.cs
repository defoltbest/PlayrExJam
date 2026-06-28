using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Scene2D5Bootstrap))]
public class Scene2D5BootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Планировка: Hallway, Bathroom, Kitchen, Living, Bedroom + Stairwell (подъезд).\n" +
            "Двери — группа Doors (петля + Panel с Door).\n" +
            "Rebuild пересобирает всю квартиру заново (включая лестницу и пол подъезда).\n" +
            "Клик по двери в Play Mode открывает её через Input System (PlayerController → CursorHit → Door).",
            MessageType.Info);

        var bootstrap = (Scene2D5Bootstrap)target;

        if (GUILayout.Button("Rebuild Apartment From Template", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                    "Пересоздать квартиру?",
                    "Объекты внутри House будут удалены и создана новая сетка комнат.",
                    "Пересоздать",
                    "Отмена"))
            {
                Undo.RegisterFullObjectHierarchyUndo(bootstrap.gameObject, "Rebuild Apartment");
                bootstrap.RebuildHouseFromTemplate();
                EditorUtility.SetDirty(bootstrap.gameObject);
            }
        }

        if (GUILayout.Button("Apply Isometric Camera Preset", GUILayout.Height(24)))
        {
            Undo.RegisterFullObjectHierarchyUndo(bootstrap.gameObject, "Apply Isometric Camera");
            bootstrap.ApplyCameraPreset();
            EditorUtility.SetDirty(bootstrap.gameObject);
        }
    }
}
