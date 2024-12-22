/*******************************************************
 * Script Name   : CinemachineUpgradeData.cs
 * Author        : Murat Gokce
 * Created Date  : 12.22.2024
 *
 * © Murat Gökçe, 2024. All rights reserved.
 * This script is developed by Murat Gökçe and 
 * unauthorized copying or distribution is prohibited.
 *******************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace CinemachineUpgrader
{
    [CreateAssetMenu(fileName = "CinemachineUpgradeData", menuName = "Cinemachine/Upgrade Data")]
    public class CinemachineUpgradeData : ScriptableObject
    {
        [System.Serializable]
        public class ComponentFieldReplacement
        {
            public string oldFieldName;
            public string newFieldName;
        }

        [System.Serializable]
        public class ComponentData
        {
            public string oldComponentName;
            public string newComponentName;
            public List<ComponentFieldReplacement> fieldReplacements = new List<ComponentFieldReplacement>();
        }

        // Cinemachine components (2.X) that are known to be used in the project
        public List<string> knownComponents = new List<string>();

        // Component mappings for upgrade process
        public List<ComponentData> componentMappings = new List<ComponentData>();

        // Namespace replacements
        public List<string> namespaceReplacements = new List<string>();

        // Method name replacements
        public List<string> methodReplacements = new List<string>();

        private void OnEnable()
        {
            InitializeDefaultValuesIfEmpty();
        }

        private void OnValidate()
        {
            InitializeDefaultValuesIfEmpty();
        }

        private void InitializeDefaultValuesIfEmpty()
        {
            // Initialize known components if empty
            if (knownComponents == null || knownComponents.Count == 0)
            {
                knownComponents = new List<string>
                {
                    // Cinemachine 2.X components
                    "CinemachineVirtualCamera",
                    "CinemachineFreeLook",
                    "CinemachinePath",
                    "CinemachineSmoothPath",
                    "CinemachineDollyCart",
                    "CinemachineTransposer",
                    "CinemachineOrbitalTransposer",
                    "CinemachineFramingTransposer",
                    "CinemachineComposer",
                    "CinemachinePOV",
                    "CinemachineTrackedDolly",
                    "CinemachineGroupComposer",
                    "CinemachineCollider",
                    "CinemachineConfiner",
                    "Cinemachine3rdPersonFollow",
                    "Cinemachine3rdPersonAim",
                    "CinemachineBlendListCamera",
                    "CinemachineBrain",
                    "CinemachineExternalCamera",
                    "CinemachineFollowZoom",
                    "CinemachineHardLockToTarget",
                    "CinemachineHardLookAt",
                    "CinemachineOrbitalFollow",
                    "CinemachinePipeline",
                    "CinemachineSameAsFollowTarget",
                    "CinemachineSplineCart",
                    "CinemachineStoryboard",
                    "CinemachineTargetGroup",
                    "CinemachinePixelPerfect",
                    "CinemachineMixingCamera",
                    "CinemachineRecomposer",
                    "CinemachineClearShot",
                    "CinemachineStateDriver",
                    "CinemachineBrain.UpdateMethod",
                    "CinemachineBlendDefinition.Style"
                };
            }

            // Initialize component mappings if empty
            if (componentMappings == null || componentMappings.Count == 0)
            {
                componentMappings = new List<ComponentData>
                {
                    CreateComponentData("CinemachineVirtualCamera", "CinemachineCamera"),
                    CreateComponentData("CinemachineFreeLook", "CinemachineCamera"),
                    CreateComponentData("CinemachinePath", "SplineContainer"),
                    CreateComponentData("CinemachineSmoothPath", "SplineContainer"),
                    CreateComponentData("CinemachineDollyCart", "CinemachineSplineCart", new Dictionary<string, string> { { "Position", "SplinePosition" } }),
                    CreateComponentData("CinemachineTransposer", "CinemachineFollow"),
                    CreateComponentData("CinemachineOrbitalTransposer", "CinemachineOrbitalFollow"),
                    CreateComponentData("CinemachineFramingTransposer", "CinemachinePositionComposer"),
                    CreateComponentData("CinemachineComposer", "CinemachineRotationComposer"),
                    CreateComponentData("CinemachinePOV", "CinemachinePanTilt"),
                    CreateComponentData("CinemachineTrackedDolly", "CinemachineSplineDolly"),
                    CreateComponentData("CinemachineGroupComposer", "CinemachineGroupFraming"),
                    CreateComponentData("CinemachineCollider", "CinemachineDeoccluder"),
                    CreateComponentData("CinemachineConfiner", "CinemachineConfiner3D"),
                    CreateComponentData("Cinemachine3rdPersonFollow", "CinemachineThirdPersonFollow"),
                    CreateComponentData("Cinemachine3rdPersonAim", "CinemachineThirdPersonAim"),
                    CreateComponentData("CinemachineBlendListCamera", "CinemachineSequencerCamera"),
                    CreateComponentData("CinemachineBrain.UpdateMethod", "CinemachineBrain.UpdateMethods"),
                    CreateComponentData("CinemachineBlendDefinition.Style", "CinemachineBlendDefinition.Styles")
                };
            }

            // Initialize namespace replacements if empty
            if (namespaceReplacements == null || namespaceReplacements.Count == 0)
            {
                namespaceReplacements = new List<string>
                {
                    "using Cinemachine;|using Unity.Cinemachine;",
                    "using Cinemachine.Editor;|using Unity.Cinemachine.Editor;",
                    "using Cinemachine.Utility;|using Unity.Cinemachine;"
                };
            }

            // Initialize method replacements if empty
            if (methodReplacements == null || methodReplacements.Count == 0)
            {
                methodReplacements = new List<string>
                {
                    "SimpleFollowWithWorldUp|LazyFollow",
                    "GetCinemachineComponent|GetComponent"
                };
            }
        }

        private ComponentData CreateComponentData(string oldName, string newName, Dictionary<string, string> fieldReplacements = null)
        {
            var data = new ComponentData
            {
                oldComponentName = oldName,
                newComponentName = newName,
                fieldReplacements = new List<ComponentFieldReplacement>()
            };

            if (fieldReplacements != null)
            {
                foreach (var replacement in fieldReplacements)
                {
                    data.fieldReplacements.Add(new ComponentFieldReplacement
                    {
                        oldFieldName = replacement.Key,
                        newFieldName = replacement.Value
                    });
                }
            }

            return data;
        }
    }
}