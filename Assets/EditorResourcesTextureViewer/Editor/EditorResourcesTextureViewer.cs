using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;

#nullable enable

namespace jwelloneEditor
{
    public sealed class EditorResourcesTextureViewer : EditorWindow
    {
        [SerializeField] int _selectIndex;
        [SerializeField] int _buttonSize = 32;
        [SerializeField] Vector2 _scrollPosition;
        [SerializeField]string _filter = string.Empty;
        [NonSerialized] SearchField? _searchField;
        readonly List<Texture2D> _cacheTextures = new();
        readonly List<Texture2D> _displayTextures = new();

        [MenuItem("Tools/jwellone/Editor Resources Viewer")]
        static void Init()
        {
            var window = EditorWindow.GetWindow(typeof(EditorResourcesTextureViewer));
            window.Show();
        }

        void OnEnable()
        {
            var assets = Resources.FindObjectsOfTypeAll(typeof(Texture2D))
                .Where(x => AssetDatabase.GetAssetPath(x) == "Library/unity editor resources")
                .Select(x => x.name)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => EditorGUIUtility.Load(x) as Texture2D)
                .Where(x => x)
                .ToArray();

            _cacheTextures.Clear();
            _cacheTextures.AddRange(assets!);

            _displayTextures.Clear();
            _displayTextures.AddRange(assets!);

            _filter = string.Empty;
        }

        void OnDisable()
        {
            _cacheTextures.Clear();
            _displayTextures.Clear();
        }

        void OnGUI()
        {
            var texture = _selectIndex < _displayTextures.Count ? _displayTextures[_selectIndex] : null;

            EditorGUILayout.BeginVertical();

            GUILayout.BeginHorizontal(GUILayout.Height(position.size.y / 2f));

            DrawTexture(texture);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            _buttonSize = EditorGUILayout.IntSlider(_buttonSize, 24, 128);

            DrawSearchField();

            GUI.enabled = _displayTextures.Count > 0;
            if (GUILayout.Button("Export All", GUILayout.Width(64)))
            {
                var filePath = EditorUtility.SaveFolderPanel("Unity Editor Resources", Application.dataPath, "");
                ExportAll(_displayTextures, filePath);
            }
            GUI.enabled = true;

            GUI.enabled = texture != null;
            if (GUILayout.Button("Export", GUILayout.Width(48)))
            {
                var filePath = EditorUtility.SaveFilePanel("Unity Editor Resources", Application.dataPath, $"{texture!.name}.png", ".png");
                Export(texture, filePath);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            DrawButtonArea();

            EditorGUILayout.EndVertical();
        }

        void UpdateData()
        {
            _selectIndex = 0;
            _displayTextures.Clear();
            foreach (var texture in _cacheTextures)
            {
                if (!string.IsNullOrEmpty(_filter) && !texture.name.ToLower().Contains(_filter.ToLower()))
                {
                    continue;
                }

                _displayTextures.Add(texture);
            }
        }

        void DrawSearchField()
        {
            _searchField ??= new SearchField();
            var filter = _searchField.OnToolbarGUI(_filter);
            if (filter != _filter)
            {
                _filter = filter;
                UpdateData();
            }
        }

        void DrawTexture(Texture? texture)
        {
            var width = position.size.x;
            var height = position.size.y / 2f;
            var texWidth = (float)((width > height) ? height : width);
            var texHeight = texWidth * texWidth / texWidth;

            if (texture != null)
            {
                if (texture.width > texture.height)
                {
                    texHeight *= texture.height / (float)texture.width;
                }
                else
                {
                    texWidth *= texture.width / (float)texture.height;
                }
            }

            var spaceWidth = (width - texWidth) / 2f;
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(spaceWidth);

            var spaceHeight = (height - texHeight) / 2f;
            GUILayout.BeginVertical();
            GUILayout.Space(spaceHeight);

            var textureRect = GUILayoutUtility.GetRect(texWidth, texHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            GUI.DrawTexture(textureRect, texture);

            textureRect.x = 4;
            textureRect.width = width;
            textureRect.y += texHeight / 2f + spaceHeight - 8;
            GUI.Label(textureRect, $"{texture?.name} , {texture?.width} x {texture?.height} , {texture?.graphicsFormat} , {texture?.filterMode}");

            GUILayout.Space(spaceHeight);
            GUILayout.EndVertical();

            GUILayout.Space(spaceWidth);
            GUILayout.EndHorizontal();
        }

        void DrawButtonArea()
        {
            EditorGUILayout.Space(1);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                var xNum = Mathf.FloorToInt(position.width / (_buttonSize + 3.5f));
                var yNum = xNum < _displayTextures.Count ? _displayTextures.Count / (xNum - 1) : 1;
                var index = 0;
                var btnContent = new GUIContent();
                for (var y = 0; y < yNum; ++y)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (var x = 0; x < xNum && index < _displayTextures.Count; ++x, ++index)
                    {
                        var texture = _displayTextures[index];
                        btnContent.image = texture;
                        btnContent.tooltip = texture.name;

                        if (GUILayout.Button(btnContent, GUILayout.Width(_buttonSize), GUILayout.Height(_buttonSize)))
                        {
                            _selectIndex = index;
                            GUIUtility.systemCopyBuffer = texture.name;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        void ExportAll(List<Texture2D> textures, string path)
        {
            foreach (var texture in textures)
            {
                Export(texture, Path.Combine(path, $"{texture.name}.png"));
            }
        }

        void Export(Texture2D source, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var texture = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
            Graphics.CopyTexture(source, texture);
            var bytes = default(byte[]);
            switch (Path.GetExtension(path))
            {
                case ".jpg":
                case ".jpeg":
                    bytes = texture.EncodeToJPG();
                    break;

                case ".tga":
                    bytes = texture.EncodeToTGA();
                    break;

                default:
                    bytes = texture.EncodeToPNG();
                    break;
            }

            DestroyImmediate(texture);
            File.WriteAllBytes(path, bytes);

            if (path.StartsWith(Application.dataPath))
            {
                AssetDatabase.ImportAsset(path.Replace(Application.dataPath, "Assets"));
            }
        }
    }
}