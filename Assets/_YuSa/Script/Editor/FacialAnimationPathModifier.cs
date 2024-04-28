using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;

public class FacialAnimationPathModifier : EditorWindow
{
    private string name;
    private DefaultAsset folderPath;

    [UnityEditor.MenuItem("YuSa64/Facial Path Modifier")]
    public static void ShowWindow()
    {
        GetWindow<FacialAnimationPathModifier>("Facial Path Modifier");
    }

    private void OnGUI()
    {
        name = EditorGUILayout.TextField("얼굴 메쉬 이름", name);
        folderPath = (DefaultAsset)EditorGUILayout.ObjectField("폴더", folderPath, typeof(DefaultAsset), false);

        if (GUILayout.Button("패스 수정"))
        {
            ModifyPaths();
        }

        GUILayout.Label("패스 수정이 완료되고 나면,\n대상 폴더 내의 converted 폴더에 변환된 애니메이션들이 있습니다. \nBy YuSa64", EditorStyles.helpBox);
    }

    private void ModifyPaths()
    {
        if (string.IsNullOrEmpty(name))
        {
            EditorUtility.DisplayDialog("Error", "얼굴 메쉬의 이름을 입력해주세요.", "OK");
            return;
        }
        if (folderPath != null)
        {
            string assetFolderPath = AssetDatabase.GetAssetPath(folderPath);
            string convertedFolderPath = Path.Combine(assetFolderPath, "converted");

            // Create the 'converted' folder if it doesn't exist
            if (!Directory.Exists(convertedFolderPath))
            {
                Directory.CreateDirectory(convertedFolderPath);
            }

            // Get all .anim files in the folder
            string[] animFiles = Directory.GetFiles(assetFolderPath, "*.anim", SearchOption.AllDirectories);

            foreach (string filePath in animFiles)
            {
                // Ignore files already in the 'converted' folder
                if (filePath.StartsWith(convertedFolderPath))
                {
                    continue;
                }

                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(convertedFolderPath, fileName);

                // Duplicate the .anim file to the 'converted' folder using AssetDatabase.CopyAsset
                string sourcePath = filePath.Replace(Application.dataPath, "Assets");
                string destinationPath = destFilePath.Replace(Application.dataPath, "Assets");

                if (AssetDatabase.CopyAsset(sourcePath, destinationPath))
                {
                    // Modify the paths in the duplicated file
                    ModifyFile(destinationPath);
                }
                else
                {
                    Debug.LogError("Failed to copy asset: " + sourcePath);
                }
            }

            // Refresh the AssetDatabase after modifying the files
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", "패스 수정이 완료되었습니다.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "애님 파일이 들어있는 폴더를 선택해주세요.", "OK");
        }
    }

    private void ModifyFile(string assetPath)
    {
        string[] lines = File.ReadAllLines(assetPath);

        for (int i = 0; i < lines.Length; i++)
        {
            // Replace Neutral with the face mesh name
            lines[i] = lines[i].Replace("Neutral", name);

            // Fix blendShape paths
            lines[i] = lines[i].Replace("blendShape1.", "");
        }

        // Write the modified content back to the file
        File.WriteAllLines(assetPath, lines);
    }
}