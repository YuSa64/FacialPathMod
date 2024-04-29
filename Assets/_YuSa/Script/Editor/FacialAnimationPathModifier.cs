using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;
using System;
using System.Linq;

public class FacialAnimationPathModifier : EditorWindow
{
    private GameObject faceMesh;
    private string name;
    private bool inputMeshName = false;
    private DefaultAsset folderPath;

    private bool isEyeBone = false;
    private bool inputPath = false;
    private GameObject leftEye;
    private GameObject rightEye;
    private string leftEyePath;
    private string rightEyePath;

    [UnityEditor.MenuItem("YuSa64/Facial Path Modifier")]
    public static void ShowWindow()
    {
        GetWindow<FacialAnimationPathModifier>("Facial Path Modifier");
    }

    private void OnGUI()
    {
        inputMeshName = EditorGUILayout.Toggle("직접 입력하기", inputMeshName);
        if(!inputMeshName)
        {
            faceMesh = (GameObject)EditorGUILayout.ObjectField("얼굴 메쉬", faceMesh, typeof(GameObject), true);
            name = faceMesh != null ? faceMesh.name : "";
        } else {
            name = EditorGUILayout.TextField("얼굴 메쉬 이름", name);
        }
        isEyeBone = EditorGUILayout.Toggle("눈이 본으로 움직일 경우", isEyeBone);
        if (isEyeBone)
        {
            inputPath = EditorGUILayout.Toggle("본 경로 직접 입력하기", inputPath);
            if(!inputPath)
            {
                leftEye = (GameObject)EditorGUILayout.ObjectField("왼쪽 눈", leftEye, typeof(GameObject), true);
                rightEye = (GameObject)EditorGUILayout.ObjectField("오른쪽 눈", rightEye, typeof(GameObject), true);
                leftEyePath = (leftEye != null) ? AnimationUtility.CalculateTransformPath(leftEye.transform, leftEye.transform.root) : "";
                rightEyePath = (rightEye != null) ? AnimationUtility.CalculateTransformPath(rightEye.transform, rightEye.transform.root) : "";
            } else {
                leftEyePath = EditorGUILayout.TextField("왼쪽 눈 경로", leftEyePath);
                rightEyePath = EditorGUILayout.TextField("오른쪽 눈 경로", rightEyePath);
            }
        }
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
            if(inputMeshName
    ) EditorUtility.DisplayDialog("Error", "얼굴 메쉬 이름을 입력해주세요.", "OK");
            else EditorUtility.DisplayDialog("Error", "얼굴 메쉬를 지정해주세요.", "OK");
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

            string[] fbxFiles = Directory.GetFiles(assetFolderPath, "*.fbx", SearchOption.AllDirectories);

            foreach (string filePath in fbxFiles)
            {
                ExtractAnim(assetFolderPath, filePath);
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

            // Fix eye bone paths
            if (isEyeBone)
            {
                lines[i] = lines[i].Replace("Body/eye_grp/eyeBall_L", leftEyePath);
                lines[i] = lines[i].Replace("Body/eye_grp/eyeBall_R", rightEyePath);
            }

        }

        // Write the modified content back to the file
        File.WriteAllLines(assetPath, lines);
    }

    private void ExtractAnim(string assetFolderPath, string assetPath)
    {
        // Load all assets at the path
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        // Filter the assets by type
        AnimationClip[] clips = Array.FindAll(allAssets, asset => asset is AnimationClip).Cast<AnimationClip>().ToArray();
        
        foreach (AnimationClip clip in clips)
        {
            AnimationClip newClip = Object.Instantiate(clip);
            if(newClip == null || newClip.name == "" || newClip.name.Contains("__preview__")) continue;
            string fbxName = Path.GetFileNameWithoutExtension(assetPath);
            string clipPath = Path.Combine(assetFolderPath, fbxName + "_" + newClip.name + ".anim");
            AssetDatabase.CreateAsset(newClip, clipPath);
        }
    }
}
