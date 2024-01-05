using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace OccaSoftware.TextureChannelPacker.Editor
{
    public class TextureChannelPackerWindow : EditorWindow
    {
        private Vector2Int Resolution = new Vector2Int(512, 512);
        private List<TextureData> textureDatas = new List<TextureData>(4);
        private Texture2D previewOutput = null;
        private Vector2 scrollPosition;
        private FileType fileType = FileType.PNG;
        private string directory = "Assets";
        private string fileName = "BakedTexture";

        private PreviewMode previewMode = PreviewMode.RGB;

        private enum FileType
        {
            PNG,
            JPEG,
            EXR,
            TGA
        }

        private enum PreviewMode
        {
            RGB,
            R,
            G,
            B,
            A
        }

        [MenuItem("Tools/OccaSoftware/Texture Channel Packer")]
        static void OpenWindow()
        {
            //Create window
            TextureChannelPackerWindow window = GetWindow<TextureChannelPackerWindow>(
                title: "Channel Packer"
            );
            window.Show();
        }

        private void OnEnable()
        {
            textureDatas.Clear();
            textureDatas.Add(new TextureData());
            textureDatas.Add(new TextureData());
            textureDatas.Add(new TextureData());
            textureDatas.Add(new TextureData());
        }

        private void OnDisable()
        {
            previewOutput = null;
            textureDatas.Clear();
            previewMode = PreviewMode.RGB;
        }

        private static Color RGB255ToRGB01(int r, int g, int b, float alpha)
        {
            return new Color((float)r / 255, (float)g / 255, (float)b / 255, alpha);
        }

        private struct Channel
        {
            public string name;
            public Color color;

            public Channel(string name, int r, int g, int b, float alpha)
            {
                this.name = name;
                this.color = RGB255ToRGB01(r, g, b, alpha);
            }
        }

        private void DrawChannel(TextureData textureData, Channel channel)
        {
            GUIStyle style = new GUIStyle();
            style.padding = new RectOffset(10, 10, 0, 0);
            Rect r = EditorGUILayout.BeginVertical(style);
            r.x += 5;
            r.width -= 10;

            EditorGUI.DrawRect(r, channel.color);
            EditorGUILayout.LabelField(channel.name + " Channel", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            textureData.texture = (Texture2D)
                EditorGUILayout.ObjectField(textureData.texture, typeof(Texture2D), false);
            textureData.textureChannelSource = (TextureData.TextureChannelSource)
                EditorGUILayout.EnumPopup("Source", textureData.textureChannelSource);
            textureData.invert = EditorGUILayout.Toggle("Invert", textureData.invert);
            textureData.clearValue = EditorGUILayout.FloatField(
                "Clear Value",
                textureData.clearValue
            );
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPreview()
        {
            if (previewOutput != null)
            {
                GUILayout.FlexibleSpace();
                GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.MinHeight(256) };
                EditorGUILayout.BeginVertical(options);

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                Rect headerRect = EditorGUILayout.GetControlRect(
                    false,
                    EditorGUIUtility.singleLineHeight
                );
                Rect label = headerRect;
                label.width = 100;
                EditorGUI.LabelField(label, "Preview", EditorStyles.boldLabel);

                GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
                buttonStyle.fixedWidth = 30;
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.fontSize = 10;
                previewMode = GUILayout.Toggle(previewMode == PreviewMode.RGB, "RGB", buttonStyle)
                    ? PreviewMode.RGB
                    : previewMode;
                previewMode = GUILayout.Toggle(previewMode == PreviewMode.R, "R", buttonStyle)
                    ? PreviewMode.R
                    : previewMode;
                previewMode = GUILayout.Toggle(previewMode == PreviewMode.G, "G", buttonStyle)
                    ? PreviewMode.G
                    : previewMode;
                previewMode = GUILayout.Toggle(previewMode == PreviewMode.B, "B", buttonStyle)
                    ? PreviewMode.B
                    : previewMode;
                previewMode = GUILayout.Toggle(previewMode == PreviewMode.A, "A", buttonStyle)
                    ? PreviewMode.A
                    : previewMode;
                EditorGUILayout.EndHorizontal();

                options = new GUILayoutOption[]
                {
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true)
                };
                Rect rect = EditorGUILayout.GetControlRect(false, options);

                UnityEngine.Rendering.ColorWriteMask writeMask = UnityEngine
                    .Rendering
                    .ColorWriteMask
                    .All;

                switch (previewMode)
                {
                    case PreviewMode.R:
                        writeMask = UnityEngine.Rendering.ColorWriteMask.Red;
                        break;
                    case PreviewMode.G:
                        writeMask = UnityEngine.Rendering.ColorWriteMask.Green;
                        break;
                    case PreviewMode.B:
                        writeMask = UnityEngine.Rendering.ColorWriteMask.Blue;
                        break;
                }

                if (previewMode == PreviewMode.A)
                {
                    EditorGUI.DrawTextureAlpha(rect, previewOutput, ScaleMode.ScaleToFit, 0, 0);
                }
                else
                {
                    EditorGUI.DrawPreviewTexture(
                        rect,
                        previewOutput,
                        null,
                        ScaleMode.ScaleToFit,
                        0,
                        0,
                        writeMask
                    );
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSaveOptions()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            fileType = (FileType)EditorGUILayout.EnumPopup("File Format", fileType);

            Rect buttonRect = EditorGUILayout.GetControlRect(
                false,
                EditorGUIUtility.singleLineHeight * 3
            );
            buttonRect = EditorGUI.IndentedRect(buttonRect);
            if (GUI.Button(buttonRect, "Save"))
            {
                BakeTexture();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 18;
            EditorGUILayout.LabelField("Texture Channel Packer", headerStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Add the textures you want to bake. Set the source channel for the texture. Set the size and location of the texture you want to bake to. Press the \"Bake\" button.",
                MessageType.Info
            );
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            float alpha = 0.3f;
            DrawChannel(textureDatas[0], new Channel("Red", 231, 29, 54, alpha));
            DrawChannel(textureDatas[1], new Channel("Green", 158, 189, 110, alpha));
            DrawChannel(textureDatas[2], new Channel("Blue", 109, 152, 186, alpha));
            DrawChannel(textureDatas[3], new Channel("Alpha", 226, 194, 198, alpha));

            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                ValidateInputs();
                GeneratePreview();
            }

            DrawSaveOptions();

            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        private void GeneratePreview()
        {
            if (
                textureDatas[0].texture == null
                && textureDatas[1].texture == null
                && textureDatas[2].texture == null
                && textureDatas[3].texture == null
            )
                return;

            SetupResolution();
            ExtractColorData();

            previewOutput = new Texture2D(Resolution.x, Resolution.y, TextureFormat.RGBA32, true);

            List<Color> finalColors = new List<Color>();
            for (int i = 0; i < Resolution.x * Resolution.y; i++)
            {
                finalColors.Add(
                    new Color(
                        textureDatas[0].extractedColorDataFromChannel[i],
                        textureDatas[1].extractedColorDataFromChannel[i],
                        textureDatas[2].extractedColorDataFromChannel[i],
                        textureDatas[3].extractedColorDataFromChannel[i]
                    )
                );
            }

            previewOutput.SetPixels(finalColors.ToArray());
            previewOutput.Apply();
        }

        private void ValidateInputs()
        {
            foreach (TextureData textureData in textureDatas)
            {
                textureData.Validate();
            }
        }

        private void SetupResolution()
        {
            Resolution.x = 1;
            Resolution.y = 1;
            foreach (TextureData textureData in textureDatas)
            {
                if (textureData.texture != null)
                {
                    Resolution.x = textureData.texture.width;
                    Resolution.y = textureData.texture.height;
                    break;
                }
            }
        }

        private void ExtractColorData()
        {
            foreach (TextureData textureData in textureDatas)
            {
                textureData.ExtractColorData(Resolution);
            }
        }

        private void BakeTexture()
        {
            string filePath = EditorUtility.SaveFilePanelInProject(
                "Save As",
                fileName,
                fileType.ToString().ToLowerInvariant(),
                "",
                directory
            );

            try
            {
                directory = Path.GetDirectoryName(filePath);
                fileName = Path.GetFileNameWithoutExtension(filePath);
            }
            catch (ArgumentException) { }

            if (filePath.Length == 0)
            {
                return;
            }

            if (GetByteArrayByPath(previewOutput, out byte[] output))
            {
                if (output == null)
                    return;

                File.WriteAllBytes(filePath, output);
                AssetDatabase.Refresh();
            }
        }

        private bool GetByteArrayByPath(Texture2D tex, out byte[] output)
        {
            switch (fileType)
            {
                case FileType.PNG:
                    output = tex.EncodeToPNG();
                    return true;
                case FileType.JPEG:
                    output = tex.EncodeToJPG();
                    return true;
                case FileType.EXR:
                    output = tex.EncodeToEXR();
                    return true;
                case FileType.TGA:
                    output = tex.EncodeToTGA();
                    return true;
            }

            output = null;
            return false;
        }
    }
}
