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
            "Каждая комната — группа в House; стены в Walls_Outer / Walls_Inner.\n" +
            "Дверные проёмы одинаковой ширины (doorWidth). Двигайте/добавляйте — затем Ctrl+S.\n" +
            "Rebuild пересобирает всю квартиру заново по шаблону.",
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
