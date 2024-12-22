/*******************************************************
 * Script Name   : CinemachineUpgraderTool.cs
 * Author        : Murat Gokce
 * Created Date  : 12.22.2024
 *
 * © Murat Gökçe, 2024. All rights reserved.
 * This script is developed by Murat Gökçe and 
 * unauthorized copying or distribution is prohibited.
 *******************************************************/

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace CinemachineUpgrader
{
    public class CinemachineUpgraderTool : EditorWindow
    {
        private string projectPath;
        private bool includeSubfolders = true;
        private bool hasErrors;
        private bool showBackupOptions;
        private bool backupFiles = true;
        private readonly bool showLogs = true;
        private Vector2 scrollPosition;

        [SerializeField] private CinemachineUpgradeData upgradeData;
        private SerializedObject serializedObject;
        private SerializedProperty upgradeDataProperty;
        
        private const string PROCESSED_FILES_KEY = "CinemachineUpgrader_ProcessedFiles";

        [System.Serializable]
        private class SerializableStringList
        {
            public List<string> items = new List<string>();
        }

        private List<string> ProcessedFiles
        {
            get
            {
                string filesJson = EditorPrefs.GetString(PROCESSED_FILES_KEY, string.Empty);
                if (string.IsNullOrEmpty(filesJson))
                {
                    return new List<string>();
                }

                try
                {
                    var data = JsonUtility.FromJson<SerializableStringList>(filesJson);
                    return data?.items ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                var wrapper = new SerializableStringList { items = value };
                string json = JsonUtility.ToJson(wrapper);
                EditorPrefs.SetString(PROCESSED_FILES_KEY, json);
            }
        }

        [MenuItem("Tools/Cinemachine Upgrader")]
        public static void ShowWindow()
        {
            GetWindow<CinemachineUpgraderTool>("Cinemachine Upgrader");
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            upgradeDataProperty = serializedObject.FindProperty(nameof(upgradeData));
        }

        private void OnGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Version Info
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Cinemachine 2.X to 3.X Upgrader", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(upgradeDataProperty);

                if (upgradeData == null)
                {
                    EditorGUILayout.HelpBox("Please assign or create Cinemachine Upgrade Data!", MessageType.Warning);

                    if (GUILayout.Button("Create New Upgrade Data"))
                    {
                        CreateNewUpgradeData();
                    }

                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }

            EditorGUILayout.Space(10);
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Script Processing Settings", EditorStyles.boldLabel);

                projectPath = EditorGUILayout.TextField("Scripts Folder Path:", projectPath);
                if (GUILayout.Button("Browse"))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Scripts Folder", "", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        projectPath = path;
                    }
                }

                includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
            }

            EditorGUILayout.Space(10);

            // Upgrade Process Section
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Upgrade Process", EditorStyles.boldLabel);

                GUI.enabled = !string.IsNullOrEmpty(projectPath) && upgradeData != null;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Step 1: Analysis and Upgrade", EditorStyles.boldLabel);
                    if (GUILayout.Button("Analyze Scripts"))
                    {
                        AnalyzeScripts();
                    }

                    if (GUILayout.Button("Upgrade to Latest Cinemachine 3.X"))
                    {
                        UpdateCinemachinePackage();
                    }
                }

                EditorGUILayout.Space(5);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Step 2: Upgrade 2.X Scripts", EditorStyles.boldLabel);
                    if (GUILayout.Button("Upgrade Scripts"))
                    {
                        UpgradeScripts();
                    }
                }

                EditorGUILayout.Space(5);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Step 3: Component Upgrade", EditorStyles.boldLabel);
                    if (GUILayout.Button("Upgrade Components"))
                    {
                        UpgradeComponents();
                    }
                }

                GUI.enabled = true;
            }

            EditorGUILayout.Space(10);

            // Backup Options Section
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showBackupOptions = EditorGUILayout.Foldout(showBackupOptions, "Backup Options", true);
                if (showBackupOptions)
                {
                    EditorGUI.indentLevel++;
                    backupFiles = EditorGUILayout.Toggle("Create Backup Files", backupFiles);

                    bool hasBackups = HasBackupFiles();
                    GUI.enabled = hasBackups;

                    if (GUILayout.Button("Restore From Backup"))
                    {
                        HandleRestoreFromBackup();
                    }

                    if (GUILayout.Button("Delete Processed Backup Files"))
                    {
                        HandleDeleteProcessedBackups();
                    }

                    GUI.enabled = true;

                    if (GUILayout.Button("Cleanup ALL .backup Files"))
                    {
                        HandleCleanupAllBackups();
                    }

                    EditorGUI.indentLevel--;
                }
            }

            // Processed Files List
            if (ProcessedFiles.Count > 0)
            {
                EditorGUILayout.Space(10);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Processed Files:", EditorStyles.boldLabel);
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
                    foreach (string file in ProcessedFiles)
                    {
                        EditorGUILayout.LabelField(Path.GetFileName(file));
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateCinemachinePackage()
        {
            try
            {
                UnityEditor.PackageManager.Client.Add("com.unity.cinemachine");
                AddLog("Started Cinemachine package update. Please wait for the Package Manager to complete the process.");
                EditorUtility.DisplayDialog("Package Update", "Started Cinemachine package update process.\n\nPlease wait for the Package Manager to complete the update. " + "You can check the progress in the Package Manager window.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating Cinemachine package: {ex.Message}");
                EditorUtility.DisplayDialog("Error", "Failed to update Cinemachine package. Please try updating manually through the Package Manager.", "OK");
            }
        }

        private void AddLog(string message)
        {
            if (showLogs)
            {
                Debug.Log($"{message}");
            }
        }

        private void CreateNewUpgradeData()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Upgrade Data", "CinemachineUpgradeData", "asset", "Please enter a file name to save the upgrade data");

            if (!string.IsNullOrEmpty(path))
            {
                var asset = ScriptableObject.CreateInstance<CinemachineUpgradeData>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                upgradeData = asset;
                EditorUtility.SetDirty(this);
                AddLog($"Created new upgrade data at: {path}");
            }
        }

        private void AnalyzeScripts()
        {
            hasErrors = false;
            AddLog("Starting script analysis...");

            try
            {
                Dictionary<string, HashSet<string>> componentUsage = new Dictionary<string, HashSet<string>>();
                ProcessDirectoryForAnalysis(projectPath, componentUsage);

                if (componentUsage.Count > 0)
                {
                    int totalFiles = componentUsage.Sum(x => x.Value.Count);
                    AddLog($"\nFound {totalFiles} files with Cinemachine components:");

                    foreach (var component in componentUsage.OrderBy(x => x.Key))
                    {
                        string fileNames = string.Join(", ", component.Value.Select(Path.GetFileName));
                        AddLog($"{component.Key} -> {fileNames}");
                    }
                }
                else
                {
                    AddLog("No Cinemachine components found in the selected directory.");
                }

                string message = hasErrors ? "Analysis completed with some errors. Check console for details." : "Analysis completed successfully.";

                EditorUtility.DisplayDialog("Analysis Complete", message, "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during analysis: {ex}");
                EditorUtility.DisplayDialog("Error", "Failed to analyze scripts. Check console for details.", "OK");
            }
        }

        private void ProcessDirectoryForAnalysis(string directoryPath, Dictionary<string, HashSet<string>> componentUsage)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.cs").Where(f => !Path.GetFileName(f).StartsWith("CinemachineUpgrade"));

                foreach (string file in files)
                {
                    AnalyzeFile(file, componentUsage);
                }

                if (includeSubfolders)
                {
                    foreach (string subdirectory in Directory.GetDirectories(directoryPath))
                    {
                        ProcessDirectoryForAnalysis(subdirectory, componentUsage);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error processing directory {directoryPath}: {ex.Message}");
                hasErrors = true;
            }
        }

        private void AnalyzeFile(string filePath, Dictionary<string, HashSet<string>> componentUsage)
        {
            try
            {
                if (Path.GetFileName(filePath).StartsWith("CinemachineUpgrade")) return;

                string content = File.ReadAllText(filePath);
                foreach (var componentName in upgradeData.knownComponents.Where(component => content.Contains(component)))
                {
                    componentUsage.TryAdd(componentName, new HashSet<string>());
                    componentUsage[componentName].Add(filePath);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error analyzing file {filePath}: {ex.Message}");
                hasErrors = true;
            }
        }

        private void UpgradeScripts()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Cinemachine Upgrade Warning",
                "Before proceeding with the upgrade:\n\n" +
                "1. Make sure you have backed up your entire project\n" +
                "2. All script changes will also be backed up individually\n" +
                "3. Consider committing your current changes to version control\n\n" +
                "Do you want to continue with the upgrade process?",
                "Yes, Proceed",
                "Cancel"
            );

            if (!proceed) return;
            
            hasErrors = false;
            var currentFiles = new List<string>();
            ProcessDirectory(projectPath, currentFiles);
            ProcessedFiles = currentFiles; // Processed files'ı EditorPrefs'e kaydet
            AssetDatabase.Refresh();

            string message = hasErrors ? "Upgrade completed with some errors. Check console for details." : $"Successfully processed {ProcessedFiles.Count} files.";

            EditorUtility.DisplayDialog("Complete", message, "OK");
            Repaint(); // UI'ı yenile
        }

        private void UpgradeComponents()
        {
            try
            {
                var upgradeManagerType = System.Type.GetType("Unity.Cinemachine.Editor.CinemachineUpgradeManager, Unity.Cinemachine.Editor");
                if (upgradeManagerType != null)
                {
                    var upgradeMethod = upgradeManagerType.GetMethod("UpgradeProject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (upgradeMethod != null)
                    {
                        upgradeMethod.Invoke(null, null);
                        EditorUtility.DisplayDialog("Complete", "Component upgrade completed.", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find UpgradeProject method. Make sure you have Cinemachine 3.0 installed.", "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Could not find CinemachineUpgradeManager. Make sure you have Cinemachine 3.0 installed.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during component upgrade: {ex}");
                EditorUtility.DisplayDialog("Error", "Failed to upgrade components. Check console for details.", "OK");
            }
        }

        private void ProcessDirectory(string directoryPath, List<string> currentFiles)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.cs").Where(f => !Path.GetFileName(f).StartsWith("CinemachineUpgrade"));

                foreach (string file in files)
                {
                    ProcessFile(file, currentFiles);
                }

                if (includeSubfolders)
                {
                    foreach (string subdirectory in Directory.GetDirectories(directoryPath))
                    {
                        ProcessDirectory(subdirectory, currentFiles);
                    }
                }
            }
            catch (System.Exception ex)
            {
                AddLog($"Error processing directory {directoryPath}: {ex.Message}");
                hasErrors = true;
            }
        }

        private void ProcessFile(string filePath, List<string> currentFiles)
        {
            try
            {
                AddLog($"Processing file: {filePath}");

                string content = File.ReadAllText(filePath);
                string originalContent = content;
                bool fileModified = false;

                // Create a dictionary to store variable types for all known components
                Dictionary<string, HashSet<string>> variableTypes = new Dictionary<string, HashSet<string>>();

                // First, find all variables of any known Cinemachine component type
                foreach (var componentName in upgradeData.knownComponents)
                {
                    string pattern = $@"\b{componentName}\s+(\w+)\b";
                    var matches = Regex.Matches(content, pattern);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            string varName = match.Groups[1].Value;
                            if (!variableTypes.ContainsKey(varName))
                            {
                                variableTypes[varName] = new HashSet<string>();
                            }

                            variableTypes[varName].Add(componentName);
                            AddLog($"Found component variable: {componentName} {varName}");
                        }
                    }
                }

                // Update namespace references
                foreach (var namespaceReplacement in upgradeData.namespaceReplacements)
                {
                    string[] parts = namespaceReplacement.Split('|');
                    if (parts.Length == 2 && content.Contains(parts[0]))
                    {
                        content = content.Replace(parts[0], parts[1]);
                        fileModified = true;
                        AddLog($"Replaced namespace: {parts[0]} -> {parts[1]}");
                    }
                }

                // Process component mappings and their field replacements
                foreach (var mapping in upgradeData.componentMappings)
                {
                    // Update component type references
                    if (content.Contains(mapping.oldComponentName))
                    {
                        content = Regex.Replace(content, $@"\b{mapping.oldComponentName}\b", mapping.newComponentName);
                        fileModified = true;
                        AddLog($"Replaced component: {mapping.oldComponentName} -> {mapping.newComponentName}");
                    }

                    // Process field replacements for all variables of this component type
                    foreach (var varName in variableTypes.Keys)
                    {
                        if (variableTypes[varName].Contains(mapping.oldComponentName))
                        {
                            foreach (var fieldReplacement in mapping.fieldReplacements)
                            {
                                // Replace m_Prefix fields
                                string fieldPattern = $@"{varName}\.m_{fieldReplacement.oldFieldName}\b";
                                if (Regex.IsMatch(content, fieldPattern))
                                {
                                    var oldContent = content;
                                    content = Regex.Replace(content, fieldPattern, $"{varName}.{fieldReplacement.newFieldName}");
                                    if (oldContent != content)
                                    {
                                        AddLog($"Replaced field: {varName}.m_{fieldReplacement.oldFieldName} -> {varName}.{fieldReplacement.newFieldName}");
                                        fileModified = true;
                                    }
                                }
                            }
                        }
                    }

                    // Process chained m_prefix usages
                    Dictionary<string, string> processedChains = new Dictionary<string, string>();
                    string chainPattern = $@"(\w+\.)?{mapping.oldComponentName}.*?\.m_(\w+)\.?m_?(\w+)?";
                    var chainMatches = Regex.Matches(content, chainPattern);

                    foreach (Match match in chainMatches)
                    {
                        if (processedChains.ContainsKey(match.Value)) continue;

                        string originalText = match.Value;
                        string[] parts = originalText.Split('.');

                        List<string> processedParts = new List<string>();
                        string currentComponent;

                        foreach (string part in parts)
                        {
                            if (string.IsNullOrEmpty(part)) continue;

                            string processedPart = part;
                            if (part.StartsWith("m_"))
                            {
                                processedPart = part.Substring(2);

                                // Check if this field should be replaced based on component mappings
                                var fieldReplacement = mapping.fieldReplacements.FirstOrDefault(fr => fr.oldFieldName == processedPart);
                                if (fieldReplacement != null)
                                {
                                    processedPart = fieldReplacement.newFieldName;
                                    AddLog($"Replaced chained field: {part} -> {processedPart}");
                                }
                            }

                            if (part == mapping.oldComponentName)
                            {
                                currentComponent = mapping.newComponentName;
                                processedPart = currentComponent;
                                AddLog($"Replaced chained component: {part} -> {processedPart}");
                            }

                            processedParts.Add(processedPart);
                        }

                        string replacement = string.Join(".", processedParts);
                        processedChains[originalText] = replacement;
                        content = content.Replace(originalText, replacement);
                        fileModified = true;
                        AddLog($"Replaced chain: {originalText} -> {replacement}");
                    }
                }

                // Update method names
                foreach (var methodReplacement in upgradeData.methodReplacements)
                {
                    string[] parts = methodReplacement.Split('|');
                    if (parts.Length == 2 && content.Contains(parts[0]))
                    {
                        content = Regex.Replace(content, $@"\b{parts[0]}\b", parts[1]);
                        fileModified = true;
                        AddLog($"Replaced method: {parts[0]} -> {parts[1]}");
                    }
                }

                // Handle GetComponent<ComponentType>() cases
                foreach (var componentName in upgradeData.knownComponents)
                {
                    string pattern = $@"GetComponent\s*<\s*{componentName}\s*>\s*\(\s*\)\.m_(\w+)";
                    var matches = Regex.Matches(content, pattern);
                    foreach (Match match in matches)
                    {
                        string fieldName = match.Groups[1].Value;
                        string oldValue = match.Value;
                        string newValue = match.Value.Substring(0, match.Value.Length - fieldName.Length - 2) + fieldName;
                        content = content.Replace(oldValue, newValue);
                        fileModified = true;
                        AddLog($"Replaced GetComponent field: {oldValue} -> {newValue}");
                    }
                }

                // Save changes if file was modified
                if (fileModified)
                {
                    if (backupFiles && !File.Exists(filePath + ".backup"))
                    {
                        File.WriteAllText(filePath + ".backup", originalContent);
                        AddLog($"Created backup at: {filePath}.backup");
                    }

                    File.WriteAllText(filePath, content);
                    currentFiles.Add(filePath);
                    AddLog($"Successfully processed and saved: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error processing file {filePath}: {ex.Message}");
                hasErrors = true;
            }
        }

        private bool HasBackupFiles()
        {
            return ProcessedFiles.Any(f => File.Exists(f + ".backup"));
        }

        private void HandleRestoreFromBackup()
        {
            if (EditorUtility.DisplayDialog("Restore From Backup", "This will restore all processed files from their backups. Continue?", "Yes", "No"))
            {
                RestoreFromBackup();
                AssetDatabase.Refresh();
            }
        }

        private void RestoreFromBackup()
        {
            bool restoreSuccess = true;
            var files = ProcessedFiles;
            foreach (string filePath in files)
            {
                string backupPath = filePath + ".backup";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, filePath, true);
                        File.Delete(backupPath);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error restoring {filePath}: {ex.Message}");
                        hasErrors = true;
                        restoreSuccess = false;
                    }
                }
            }

            if (restoreSuccess)
            {
                ProcessedFiles = new List<string>();
                EditorUtility.DisplayDialog("Restore Complete", "All files have been restored and backup files have been deleted.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Restore Incomplete", "Some files could not be restored. Check console for details.", "OK");
            }

            Repaint();
        }

        private void HandleDeleteProcessedBackups()
        {
            if (EditorUtility.DisplayDialog("Delete Backups", "This will delete backup files for processed files only. This cannot be undone. Continue?", "Yes", "No"))
            {
                DeleteBackupFiles();
                EditorUtility.DisplayDialog("Delete Complete", "All processed backup files have been deleted.", "OK");
            }
        }

        private void DeleteBackupFiles()
        {
            var files = ProcessedFiles.ToList();
            bool hasError = false;

            foreach (string filePath in files)
            {
                string backupPath = filePath + ".backup";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Delete(backupPath);
                        AddLog($"Deleted backup file: {backupPath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error deleting backup {backupPath}: {ex.Message}");
                        hasError = true;
                    }
                }
            }

            ProcessedFiles = new List<string>();
            hasErrors = hasError;
            Repaint();
        }

        private void HandleCleanupAllBackups()
        {
            if (EditorUtility.DisplayDialog("Cleanup All Backups", "This will search and delete ALL .backup files in the project folder.\nThis action cannot be undone!\nContinue?", "Yes", "No"))
            {
                CleanupAllBackups();
            }
        }

        private void CleanupAllBackups()
        {
            try
            {
                string dataPath = Application.dataPath;
                dataPath = dataPath.Substring(0, dataPath.Length - "/Assets".Length);

                int deletedCount = 0;
                List<string> errorFiles = new List<string>();

                // Find all .backup files in the project
                string[] files = Directory.GetFiles(dataPath, "*.backup", SearchOption.AllDirectories);

                foreach (string backupFile in files)
                {
                    try
                    {
                        File.Delete(backupFile);
                        deletedCount++;
                        AddLog($"Deleted: {backupFile}");
                    }
                    catch (System.Exception ex)
                    {
                        errorFiles.Add(backupFile);
                        Debug.LogError($"Error deleting {backupFile}: {ex.Message}");
                    }
                }

                string message = errorFiles.Count == 0 ? $"Successfully deleted {deletedCount} backup files." : $"Deleted {deletedCount} backup files.\nFailed to delete {errorFiles.Count} files. Check console for details.";

                EditorUtility.DisplayDialog("Cleanup Complete", message, "OK");
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during cleanup: {ex.Message}");
                EditorUtility.DisplayDialog("Error", "Failed to cleanup backup files. Check console for details.", "OK");
            }
        }
    }
}