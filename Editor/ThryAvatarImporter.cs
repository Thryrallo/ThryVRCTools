using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class ThryAvatarImporter : AssetPostprocessor
    {

        void OnPreprocessModel()
        {
            ModelImporter modelImporter = assetImporter as ModelImporter;
            modelImporter.materialLocation = ModelImporterMaterialLocation.External;
            modelImporter.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
        }

        void OnPostprocessModel(GameObject obj)
        {
            Debug.Log("Pos: " + obj.name);
            ModelImporter modelImporter = assetImporter as ModelImporter;
            HumanDescription rig = modelImporter.humanDescription;
            if (modelImporter.animationType == ModelImporterAnimationType.Generic && assetPath.Contains(".fbx"))
            {
            }
            else
            {
                Debug.Log(modelImporter.assetPath);
                HumanBone[] hBones = rig.human;
                Debug.Log("Human Bones: " + hBones.Length);
                foreach (HumanBone b in hBones) Debug.Log(b.humanName + ":" + b.boneName);
            }
        }

        static void CallbackSetType(string path)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;
            HumanDescription rig = modelImporter.humanDescription;
            SkeletonBone[] bones = rig.skeleton;
            Debug.Log("Bones: " + bones.Length);
            foreach (SkeletonBone b in bones) Debug.Log(b.name);
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.SaveAndReimport();
        }
    }
}